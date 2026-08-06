using Microsoft.Data.SqlClient;
using Simpleverse.Repository.Db.Meta;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Simpleverse.Repository.Db.SqlServer.Merge
{
	public static class MergeExtensions
	{
		public async static Task<int> UpsertAsync<T>(
			this IDbConnection connection,
			T entitiesToUpsert,
			Action<IEnumerable<T>, IEnumerable<T>, IEnumerable<PropertyInfo>, IEnumerable<PropertyInfo>> outputMap,
			IDbTransaction transaction = null,
			int? commandTimeout = null,
			Action<MergeKeyOptions> key = null,
			CancellationToken cancellationToken = default
		)
			where T : class
		{
			return await connection.UpsertAsync(
				entitiesToUpsert,
				transaction: transaction,
				commandTimeout: commandTimeout,
				key: key,
				outputOptions: options => options.Map = outputMap,
				cancellationToken: cancellationToken
			);
		}

		/// <param name="outputOptions">
		/// Configures the output map and, via <see cref="OutputOptions{T}.MapChangedOnly"/>, whether the
		/// matched entity is only updated (and therefore only mapped) if its columns actually differ. Set
		/// MapChangedOnly to false to update and map the entity even if unchanged.
		/// </param>
		public async static Task<int> UpsertAsync<T>(
			this IDbConnection connection,
			T entitiesToUpsert,
			IDbTransaction transaction = null,
			int? commandTimeout = null,
			Action<MergeKeyOptions> key = null,
			Action<OutputOptions<T>> outputOptions = null,
			CancellationToken cancellationToken = default
		)
			where T : class
		{
			return await connection.UpsertBulkAsync(
				new[] { entitiesToUpsert },
				transaction: transaction,
				commandTimeout: commandTimeout,
				key: key,
				outputOptions: outputOptions,
				cancellationToken: cancellationToken
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
			Action<IEnumerable<T>, IEnumerable<T>, IEnumerable<PropertyInfo>, IEnumerable<PropertyInfo>> outputMap = null,
			CancellationToken cancellationToken = default
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
				outputMap: outputMap,
				cancellationToken: cancellationToken
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
		public async static Task<int> UpsertBulkAsync<T>(
			this IDbConnection connection,
			IEnumerable<T> entitiesToUpsert,
			Action<IEnumerable<T>, IEnumerable<T>, IEnumerable<PropertyInfo>, IEnumerable<PropertyInfo>> outputMap,
			IDbTransaction transaction = null,
			int? commandTimeout = null,
			Action<SqlBulkCopy> sqlBulkCopy = null,
			Action<MergeKeyOptions> key = null,
			CancellationToken cancellationToken = default
		) where T : class
		{
			return await connection.UpsertBulkAsync(
				entitiesToUpsert,
				transaction: transaction,
				commandTimeout: commandTimeout,
				sqlBulkCopy: sqlBulkCopy,
				key: key,
				outputOptions: options => options.Map = outputMap,
				cancellationToken: cancellationToken
			);
		}

		/// <param name="outputOptions">
		/// Configures the output map and, via <see cref="OutputOptions{T}.MapChangedOnly"/>, whether matched
		/// entities are only updated (and therefore only mapped) if their columns actually differ. Set
		/// MapChangedOnly to false to update and map every matched entity, including unchanged ones.
		/// </param>
		/// <returns>true if updated, false if not found or not modified (tracked entities)</returns>
		public async static Task<int> UpsertBulkAsync<T>(
			this IDbConnection connection,
			IEnumerable<T> entitiesToUpsert,
			IDbTransaction transaction = null,
			int? commandTimeout = null,
			Action<SqlBulkCopy> sqlBulkCopy = null,
			Action<MergeKeyOptions> key = null,
			Action<OutputOptions<T>> outputOptions = null,
			CancellationToken cancellationToken = default
		) where T : class
		{
			var options = new OutputOptions<T>();
			outputOptions?.Invoke(options);

			return await connection.MergeBulkAsync(
				entitiesToUpsert,
				transaction,
				commandTimeout,
				sqlBulkCopy: sqlBulkCopy,
				key: key,
				matched: matchedOptions => matchedOptions.Update(checkConditionOnColumns: options.MapChangedOnly),
				notMatchedByTarget: notMatchedOptions => notMatchedOptions.Insert(),
				outputMap: options.Map,
				cancellationToken: cancellationToken
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
			Action<IEnumerable<T>, IEnumerable<T>, IEnumerable<PropertyInfo>, IEnumerable<PropertyInfo>> outputMap = null,
			CancellationToken cancellationToken = default
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

			var onColumns = OnColumns(typeMeta, keyAction: key);
			var onProperties = OnProperties(typeMeta, onColumns);

			return await connection.ExecuteAsync(
				entitiesToMerge,
				typeMeta.PropertiesExceptComputed,
				async (connection, source, parameters, properties) =>
				{
					var sb = new StringBuilder($@"
						MERGE INTO {typeMeta.TableName} AS Target
						USING {source} AS Source
						ON ({onColumns.ColumnListEquals(" AND ")})"
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
								index == 0 ? typeMeta.PropertiesExceptKeyAndComputed : onProperties,
								typeMeta.Properties
							);
						},
						transaction: transaction,
						commandTimeout: commandTimeout,
						outputResultsSplitConditions: new[] { "[ACTION] = 'INSERT'", "[ACTION] = 'UPDATE'" },
						cancellationToken: cancellationToken
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

		/// <summary>
		/// Resolves the columns the merge matches on back to properties, so that rows returned for
		/// matched entities can be mapped onto the entities they originated from. Matching on the merge
		/// columns instead of the key is what allows generated keys to be mapped onto updated entities,
		/// which do not necessarily carry the key when merging on other columns.
		/// </summary>
		public static IEnumerable<PropertyInfo> OnProperties(TypeMeta typeMeta, IEnumerable<string> onColumns)
		{
			if (onColumns == null)
				return typeMeta.PropertiesKeyAndExplicit;

			var properties = typeMeta.Properties
				.Where(x => onColumns.Contains(x.Name, StringComparer.OrdinalIgnoreCase))
				.ToList();

			if (properties.Count != onColumns.Count())
				return typeMeta.PropertiesKeyAndExplicit;

			return properties;
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
