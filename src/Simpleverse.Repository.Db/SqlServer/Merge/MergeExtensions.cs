using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;
using System.Linq;
using System.Reflection;
using Simpleverse.Repository.Db.Meta;
using Simpleverse.Repository.Db.SqlServer;
using System.Data.Common;

namespace Simpleverse.Repository.Db.SqlServer.Merge
{
	public static class MergeExtensions
	{
		public async static Task<int> UpsertAsync<T>(
			this DbConnection connection,
			T entitiesToUpsert,
			DbTransaction transaction = null,
			int? commandTimeout = null,
			Action<MergeKeyOptions> key = null
		)
			where T : class
		{
			return await connection.UpsertBulkAsync(
				new[] { entitiesToUpsert },
				transaction: transaction,
				commandTimeout: commandTimeout,
				key: key
			);
		}

		public async static Task<int> MergeAsync<T>(
			this DbConnection connection,
			T entitiesToMerge,
			DbTransaction transaction = null,
			int? commandTimeout = null,
			Action<MergeKeyOptions> key = null,
			Action<MergeActionOptions<T>> matched = null,
			Action<MergeActionOptions<T>> notMatchedByTarget = null,
			Action<MergeActionOptions<T>> notMatchedBySource = null
		)
			where T : class
		{
			return await connection.MergeBulkAsync(
				new[] { entitiesToMerge },
				transaction: transaction,
				commandTimeout: commandTimeout,
				key: key,
				matched: matched,
				notMatchedByTarget: notMatchedByTarget,
				notMatchedBySource: notMatchedBySource
			);
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
			this DbConnection connection,
			IEnumerable<T> entitiesToUpsert,
			DbTransaction transaction = null,
			int? commandTimeout = null,
			Action<SqlBulkCopy> sqlBulkCopy = null,
			Action<MergeKeyOptions> key = null
		) where T : class
		{
			return await connection.MergeBulkAsync(
				entitiesToUpsert,
				transaction,
				commandTimeout,
				sqlBulkCopy: sqlBulkCopy,
				key: key,
				matched: options => options.Update(),
				notMatchedByTarget: options => options.Insert()
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
			this DbConnection connection,
			IEnumerable<T> entitiesToMerge,
			DbTransaction transaction = null,
			int? commandTimeout = null,
			Action<SqlBulkCopy> sqlBulkCopy = null,
			Action<MergeKeyOptions> key = null,
			Action<MergeActionOptions<T>> matched = null,
			Action<MergeActionOptions<T>> notMatchedByTarget = null,
			Action<MergeActionOptions<T>> notMatchedBySource = null
		) where T : class
		{
			if (entitiesToMerge == null)
				throw new ArgumentNullException(nameof(entitiesToMerge));

			var entityCount = entitiesToMerge.Count();
			if (entityCount == 0)
				return 0;

			var typeMeta = TypeMeta.Get<T>();
			if (typeMeta.PropertiesKey.Count == 0 && typeMeta.PropertiesExplicit.Count == 0 && key == null)
				throw new ArgumentException("Entity must have at least one [Key] or [ExplicitKey] property");

			var result = await connection.ExecuteAsync(
				entitiesToMerge,
				typeMeta.PropertiesExceptComputed,
				async (connection, transaction, source, parameters, properties) =>
				{
					var sb = new StringBuilder($@"
						MERGE INTO {typeMeta.TableName} AS Target
						USING {source} AS Source
						ON ({OnColumns(typeMeta, keyAction: key).ColumnListEquals(" AND ")})"
					);
					sb.AppendLine();

					MergeMatchResult.Matched.Format(typeMeta, matched, sb);
					MergeMatchResult.NotMatchedBySource.Format(typeMeta, notMatchedBySource, sb);
					MergeMatchResult.NotMatchedByTarget.Format(typeMeta, notMatchedByTarget, sb);
					//MergeOutputFormat(typeMeta.PropertiesKey.Union(typeMeta.PropertiesComputed).ToList(), sb);
					sb.Append(";");

					return await connection.ExecuteAsync(sb.ToString(), param: parameters, commandTimeout: commandTimeout, transaction: transaction);
				},
				transaction: transaction,
				sqlBulkCopy: sqlBulkCopy
			);
			return result;
		}

		public static IEnumerable<string> OnColumns(TypeMeta typeMeta, Action<MergeKeyOptions> keyAction = null)
		{
			var options = new MergeKeyOptions();
			if (keyAction == null)
				options.ColumnsByPropertyInfo(typeMeta.PropertiesKeyAndExplicit);
			else
				keyAction(options);

			return options.Columns;
		}

		public static void Format<T>(this MergeMatchResult result, TypeMeta typeMeta, Action<MergeActionOptions<T>> optionsAction, StringBuilder sb)
		{
			if (optionsAction == null)
				return;

			var options = new MergeActionOptions<T>();
			optionsAction(options);
			if (options.Action == MergeAction.None)
				return;

			switch (result)
			{
				case MergeMatchResult.Matched:
					sb.AppendLine("WHEN MATCHED");
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

			options.Format(sb);
		}

		private static void MergeOutputFormat(IEnumerable<PropertyInfo> properties, StringBuilder sb)
		{
			if (properties.Any())
			{
				sb.AppendFormat("OUTPUT {0}", properties.ColumnList("Inserted"));
				sb.AppendLine();
			}
		}
	}
}
