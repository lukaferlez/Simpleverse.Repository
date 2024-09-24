using Dapper;
using MoreLinq;
using Simpleverse.Repository.Db.Meta;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Simpleverse.Repository.Db.SqlServer
{
	public static class OutputMapExtensions
	{
		public static async Task<int> ExecuteWithOutputMapAsync<T>(
			this IDbConnection connection,
			string query,
			DynamicParameters parameters,
			string outputSource,
			bool mapGeneratedValues,
			Action<int, IEnumerable<T>> map,
			IDbTransaction transaction = null,
			int? commandTimeout = null,
			IEnumerable<string> outputResultsSplitConditions = null
		)
			where T : class
		{
			return await connection.ExecuteAsyncWithTransaction(
				async (conn, tran) =>
				{
					var result = await conn.ExecuteAsync(
						query,
						param: parameters,
						commandTimeout: commandTimeout,
						transaction: tran
					);

					if (mapGeneratedValues)
					{
						var typeMeta = TypeMeta.Get<T>();

						var outputSelectQueryTemplate = $@"
							SELECT Target.*
							FROM
								(
									SELECT {typeMeta.PropertiesKeyAndExplicit.ColumnList(prefix: "OutputSource")}
									FROM {outputSource} AS OutputSource
									/**WHERE**/
									GROUP BY {typeMeta.PropertiesKeyAndExplicit.ColumnList(prefix: "OutputSource")}
								)  AS Source
								INNER JOIN {typeMeta.TableName} AS Target WITH(NOLOCK)
									ON {typeMeta.PropertiesKeyAndExplicit.ColumnListEquals(" AND ")};
						";

						if (outputResultsSplitConditions == null)
							outputResultsSplitConditions = new[] { "" };

						var outputSelectQuery = string.Join(
							Environment.NewLine,
							outputResultsSplitConditions.Select(
								x => outputSelectQueryTemplate.Replace("/**WHERE**/", string.IsNullOrWhiteSpace(x) ? "" : $"WHERE {x}")
							)
						);

						var outputs = await conn.QueryMultipleAsync(
							outputSelectQuery,
							param: parameters,
							transaction: tran,
							commandTimeout: commandTimeout
						);

						outputResultsSplitConditions.ForEach(
							(splitCondition, index) =>
							{
								map(index, outputs.Read<T>());
							}
						);
					}

					return result;
				},
				transaction: transaction
			);
		}

		public static string OutputClause(
			string targetTable = null,
			IEnumerable<PropertyInfo> properties = null,
			IEnumerable<string> arbitraryColumns = null
		)
		{
			var columns = properties?.ColumnList(prefix: "inserted");
			if (string.IsNullOrWhiteSpace(columns))
				columns = "inserted.*";

			var clause = "OUTPUT " + columns;
			if (arbitraryColumns != null)
			{
				clause += ", ";
				clause += string.Join(", ", arbitraryColumns);
			}

			if (!string.IsNullOrWhiteSpace(targetTable))
				clause += $" INTO {targetTable}";

			return clause;
		}
	}
}
