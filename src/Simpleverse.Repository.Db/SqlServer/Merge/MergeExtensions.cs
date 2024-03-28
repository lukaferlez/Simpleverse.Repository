using Dapper;
using Microsoft.Data.SqlClient;
using Simpleverse.Repository.Db.Meta;
using Simpleverse.Repository.Db.SqlServer;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Simpleverse.Repository.Db.SqlServer.Merge
{
	public static class MergeExtensions
	{
		public async static Task<int> UpsertAsync<T>(
			this IDbConnection connection,
			T entitiesToUpsert,
			IDbTransaction transaction = null,
			int? commandTimeout = null,
			Action<MergeKeyOptions> key = null,
			Action<IEnumerable<T>, IEnumerable<T>, IEnumerable<PropertyInfo>, IEnumerable<PropertyInfo>> outputMap = null
		)
			where T : class
		{
			return await connection.UpsertBulkAsync(
				new[] { entitiesToUpsert },
				transaction: transaction,
				commandTimeout: commandTimeout,
				key: key,
				outputMap: outputMap
			);
		}

		public async static Task<int> MergeAsync<T>(
			this IDbConnection connection,
			T entitiesToMerge,
			IDbTransaction transaction = null,
			int? commandTimeout = null,
			Action<MergeKeyOptions> key = null,
			Action<MergeActionOptions<T>> matched = null,
			Action<MergeActionOptions<T>> notMatchedByTarget = null,
			Action<MergeActionOptions<T>> notMatchedBySource = null,
			Action<IEnumerable<T>, IEnumerable<T>, IEnumerable<PropertyInfo>, IEnumerable<PropertyInfo>> outputMap = null
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
				notMatchedBySource: notMatchedBySource,
				outputMap: outputMap
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
			this IDbConnection connection,
			IEnumerable<T> entitiesToUpsert,
			IDbTransaction transaction = null,
			int? commandTimeout = null,
			Action<SqlBulkCopy> sqlBulkCopy = null,
			Action<MergeKeyOptions> key = null,
			Action<IEnumerable<T>, IEnumerable<T>, IEnumerable<PropertyInfo>, IEnumerable<PropertyInfo>> outputMap = null
		) where T : class
		{
			return await connection.MergeBulkAsync(
				entitiesToUpsert,
				transaction,
				commandTimeout,
				sqlBulkCopy: sqlBulkCopy,
				key: key,
				matched: options => options.Update(),
				notMatchedByTarget: options => options.Insert(),
				outputMap: outputMap
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
			this IDbConnection connection,
			IEnumerable<T> entitiesToMerge,
			IDbTransaction transaction = null,
			int? commandTimeout = null,
			Action<SqlBulkCopy> sqlBulkCopy = null,
			Action<MergeKeyOptions> key = null,
			Action<MergeActionOptions<T>> matched = null,
			Action<MergeActionOptions<T>> notMatchedByTarget = null,
			Action<MergeActionOptions<T>> notMatchedBySource = null,
			Action<IEnumerable<T>, IEnumerable<T>, IEnumerable<PropertyInfo>, IEnumerable<PropertyInfo>> outputMap = null
		) where T : class
		{
			if (entitiesToMerge == null)
				throw new ArgumentNullException(nameof(entitiesToMerge));

			var entityCount = entitiesToMerge.Count();
			if (entityCount == 0)
				return 0;

			var mapGeneratedValues = outputMap != null;
			var typeMeta = TypeMeta.Get<T>();
			if (typeMeta.PropertiesKey.Count == 0 && typeMeta.PropertiesExplicit.Count == 0 && key == null)
				throw new ArgumentException("Entity must have at least one [Key] or [ExplicitKey] property");

			var outputValues = new List<T>();
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
					if (mapGeneratedValues)
						MergeOutputFormat(sb);

					sb.Append(";");

					var query = sb.ToString();

					if (mapGeneratedValues)
					{
						var results = await connection.QueryAsync<T>(
							query,
							param: parameters,
							commandTimeout: commandTimeout,
							transaction: transaction
						);
						outputValues.AddRange(results);
						return results.Count();
					}
					else
					{
						return await connection.ExecuteAsync(
							query,
							param: parameters,
							commandTimeout: commandTimeout,
							transaction: transaction
						);
					}

				},
				transaction: transaction,
				sqlBulkCopy: sqlBulkCopy
			);

			if (mapGeneratedValues)
				outputMap(
					entitiesToMerge,
					outputValues,
					typeMeta.PropertiesExceptKeyAndComputed,
					typeMeta.PropertiesKeyAndComputed
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

		private static void MergeOutputFormat(StringBuilder sb)
		{
			sb.AppendFormat("OUTPUT inserted.*");
			sb.AppendLine();
		}
	}
}
