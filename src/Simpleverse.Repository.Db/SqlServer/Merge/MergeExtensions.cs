using Microsoft.Data.SqlClient;
using Simpleverse.Repository.Db.Meta;
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
			if (mapGeneratedValues && !typeMeta.PropertiesKeyAndExplicit.Any())
				throw new NotSupportedException("Output mapping inserted values is not supported without either a key or explicitkey");

			return await connection.ExecuteAsync(
				entitiesToMerge,
				typeMeta.PropertiesExceptComputed,
				async (connection, source, parameters, properties) =>
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

					var outputSource = source;
					var outputClause = string.Empty;
					if (mapGeneratedValues)
					{
						if (typeMeta.PropertiesKeyAndExplicit.Any())
						{
							var outputTarget = await connection.CreateTemporaryTableFromTable(
								typeMeta.TableName,
								typeMeta.Properties,
								transaction,
								arbitraryColumns: new[] { "'      ' AS [Action]" }
							);

							outputSource = outputTarget;
							outputClause = OutputMapExtensions.OutputClause(
								outputTarget,
								typeMeta.Properties,
								new[] { "$action" }
							);

							sb.Append(outputClause);
						}
					}

					sb.Append(";");

					var query = sb.ToString();

					return await connection.ExecuteWithOutputMapAsync<T>(
						query,
						parameters,
						outputSource,
						mapGeneratedValues,
						(index, values) =>
						{
							outputMap(
								entitiesToMerge,
								values,
								index == 0 ? typeMeta.PropertiesExceptKeyAndComputed : typeMeta.PropertiesKeyAndExplicit,
								typeMeta.Properties
							);
						},
						transaction: transaction,
						commandTimeout: commandTimeout,
						outputResultsSplitConditions: new[] { "[ACTION] = 'INSERT'", "[ACTION] = 'UPDATE'" }
					);
				},
				transaction: transaction,
				sqlBulkCopy: sqlBulkCopy
			);
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
