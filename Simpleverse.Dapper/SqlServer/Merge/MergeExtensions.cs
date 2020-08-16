using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;

namespace Simpleverse.Dapper.SqlServer.Merge
{
	public static class MergeExtensions
	{
		public async static Task<int> UpsertAsync<T>(
			this SqlConnection connection,
			T entitiesToUpsert,
			SqlTransaction transaction = null,
			int? commandTimeout = null
		)
			where T : class
		{
			var typeMeta = TypeMeta.Get<T>();

			return await connection.MergeAsync<T>(
				entitiesToUpsert,
				transaction,
				commandTimeout,
				matched =>
				{
					matched.Action = MergeAction.Update;
					matched.Condition = typeMeta.PropertiesExceptKeyAndComputed.ColumnListDifferenceCheck();
				},
				notMatchedByTarget =>
				{
					notMatchedByTarget.Action = MergeAction.Insert;
				}
			);
		}

		public async static Task<int> MergeAsync<T>(
			this SqlConnection connection,
			T entitiesToMerge,
			SqlTransaction transaction = null,
			int? commandTimeout = null,
			Action<MergeActionOptions> matched = null,
			Action<MergeActionOptions> notMatchedByTarget = null,
			Action<MergeActionOptions> notMatchedBySource = null
		)
			where T : class
		{
			var typeMeta = TypeMeta.Get<T>();
			if (typeMeta.PropertiesKey.Count == 0 && typeMeta.PropertiesExplicit.Count == 0)
				throw new ArgumentException("Entity must have at least one [Key] or [ExplicitKey] property");

			var wasClosed = connection.State == System.Data.ConnectionState.Closed;
			if (wasClosed) connection.Open();

			var sb = new StringBuilder($@"
				MERGE INTO {typeMeta.TableName} AS Target
				USING
				(
					VALUES({typeMeta.PropertiesExceptKeyAndComputed.ParameterList()})
				)
				ON ({typeMeta.PropertiesKeyAndExplicit.ColumnListEquals(" AND ")})"
			);

			MergeMatchResult.Matched.Format(typeMeta, matched, sb);
			MergeMatchResult.NotMatchedBySource.Format(typeMeta, notMatchedBySource, sb);
			MergeMatchResult.NotMatchedByTarget.Format(typeMeta, notMatchedByTarget, sb);
			sb.Append(";");

			var merged = await connection.ExecuteAsync(sb.ToString(), entitiesToMerge, commandTimeout: commandTimeout, transaction: transaction);

			if (wasClosed) connection.Close();

			return merged;
		}

		/// <summary>
		/// Updates entity in table "Ts", checks if the entity is modified if the entity is tracked by the Get() extension.
		/// </summary>
		/// <typeparam name="T">Type to be updated</typeparam>
		/// <param name="connection">Open SqlConnection</param>
		/// <param name="entitiesToUpsert">Entity to be updated</param>
		/// <param name="transaction">The transaction to run under, null (the default) if none</param>
		/// <param name="commandTimeout">Number of seconds before command execution timeout</param>
		/// <returns>true if updated, false if not found or not modified (tracked entities)</returns>
		public async static Task<int> UpsertBulkAsync<T>(
			this SqlConnection connection,
			IEnumerable<T> entitiesToUpsert,
			SqlTransaction transaction = null,
			int? commandTimeout = null,
			Action<SqlBulkCopy> sqlBulkCopy = null
		) where T : class
		{
			var typeMeta = TypeMeta.Get<T>();

			return await connection.MergeBulkAsync<T>(
				entitiesToUpsert,
				transaction,
				commandTimeout,
				sqlBulkCopy: sqlBulkCopy,
				matched =>
				{
					matched.Action = MergeAction.Update;
					matched.Condition = typeMeta.PropertiesExceptKeyAndComputed.ColumnListDifferenceCheck();
				},
				notMatchedByTarget =>
				{
					notMatchedByTarget.Action = MergeAction.Insert;
				}
			);
		}

		/// <summary>
		/// Merges entity in table "Ts", checks if the entity is modified if the entity is tracked by the Get() extension.
		/// </summary>
		/// <typeparam name="T">Type to be updated</typeparam>
		/// <param name="connection">Open SqlConnection</param>
		/// <param name="entitiesToMerge">Entity to be updated</param>
		/// <param name="transaction">The transaction to run under, null (the default) if none</param>
		/// <param name="commandTimeout">Number of seconds before command execution timeout</param>
		/// <returns>true if updated, false if not found or not modified (tracked entities)</returns>
		public async static Task<int> MergeBulkAsync<T>(
			this SqlConnection connection,
			IEnumerable<T> entitiesToMerge,
			SqlTransaction transaction = null,
			int? commandTimeout = null,
			Action<SqlBulkCopy> sqlBulkCopy = null,
			Action<MergeActionOptions> matched = null,
			Action<MergeActionOptions> notMatchedByTarget = null,
			Action<MergeActionOptions> notMatchedBySource = null
		) where T : class
		{
			var typeMeta = TypeMeta.Get<T>();
			if (typeMeta.PropertiesKey.Count == 0 && typeMeta.PropertiesExplicit.Count == 0)
				throw new ArgumentException("Entity must have at least one [Key] or [ExplicitKey] property");

			var wasClosed = connection.State == System.Data.ConnectionState.Closed;
			if (wasClosed) connection.Open();

			var insertedTableName = await connection.TransferBulkAsync(
				entitiesToMerge,
				typeMeta.TableName,
				typeMeta.Properties,
				transaction: transaction,
				sqlBulkCopy: sqlBulkCopy
			);

			var sb = new StringBuilder($@"
				MERGE INTO {typeMeta.TableName} AS Target
				USING {insertedTableName} AS Source
				ON ({typeMeta.PropertiesKeyAndExplicit.ColumnListEquals(" AND ")})"
			);

			MergeMatchResult.Matched.Format(typeMeta, matched, sb);
			MergeMatchResult.NotMatchedBySource.Format(typeMeta, notMatchedBySource, sb);
			MergeMatchResult.NotMatchedByTarget.Format(typeMeta, notMatchedByTarget, sb);
			sb.Append(";");

			var merged = await connection.ExecuteAsync(sb.ToString(), commandTimeout: commandTimeout, transaction: transaction);

			if (wasClosed) connection.Close();

			return merged;
		}

		private static void Format(this MergeMatchResult result, TypeMeta typeMeta, Action<MergeActionOptions> optionsAction, StringBuilder sb)
		{
			if (optionsAction == null)
				return;

			var options = new MergeActionOptions();
			optionsAction(options);
			if (options.Action == MergeAction.None)
				return;

			if (options.Columns == null)
				options.Columns = typeMeta.PropertiesExceptKeyAndComputed;

			switch (result)
			{
				case MergeMatchResult.Matched:
					sb.Append("WHEN MATCHED");
					break;
				case MergeMatchResult.NotMatchedBySource:
					sb.AppendLine("WHEN NOT MATCHED BY SOURCE");
					break;
				case MergeMatchResult.NotMatchedByTarget:
					sb.AppendLine("WHEN NOT MATCHED BY TARGET");
					break;
			}

			if (!string.IsNullOrEmpty(options.Condition))
				sb.AppendFormat(" AND ({0})", options.Condition);
			sb.AppendLine(" THEN");

			MergeActionFormat(options, sb);
		}

		private static void MergeActionFormat(MergeActionOptions options, StringBuilder sb)
		{
			switch (options.Action)
			{
				case MergeAction.Insert:
					sb.AppendFormat("INSERT({0})", options.Columns.ColumnList());
					sb.AppendLine();
					sb.AppendFormat("VALUES({0})", options.Columns.ColumnList("Source"));
					sb.AppendLine();
					break;
				case MergeAction.Update:
					sb.AppendLine("UPDATE SET");
					sb.AppendLine(options.Columns.ColumnListEquals(", ", leftPrefix: string.Empty));
					break;
				case MergeAction.Delete:
					sb.AppendLine("DELETE");
					break;
			}
		}
	}
}
