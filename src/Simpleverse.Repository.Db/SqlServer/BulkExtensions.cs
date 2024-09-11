using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.Data.SqlClient;
using MoreLinq;
using Simpleverse.Repository.Db;
using Simpleverse.Repository.Db.Meta;
using Simpleverse.Repository.Db.SqlServer;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Simpleverse.Repository.Db.SqlServer
{
	public static class BulkExtensions
	{
		public static async Task<string> TransferBulkAsync<T>(
			this IDbConnection connection,
			IEnumerable<T> entitiesToInsert,
			SqlTransaction transaction = null,
			Action<SqlBulkCopy> sqlBulkCopy = null
		)
		{
			var meta = TypeMeta.Get<T>();

			return await connection.TransferBulkAsync(
				entitiesToInsert,
				meta.TableName,
				meta.Properties,
				transaction: transaction,
				sqlBulkCopy: sqlBulkCopy
			);
		}

		/// <summary>
		/// Transfers recrods in bulk opration to sql server. Make sure you open a connection external otherwise you will lose the data you have transfered.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="connection"></param>
		/// <param name="entitiesToInsert"></param>
		/// <param name="tableName"></param>
		/// <param name="columnsToCopy"></param>
		/// <param name="transaction"></param>
		/// <param name="sqlBulkCopy"></param>
		/// <returns></returns>
		public static async Task<string> TransferBulkAsync<T>(
			this IDbConnection connection,
			IEnumerable<T> entitiesToInsert,
			string tableName,
			IEnumerable<PropertyInfo> columnsToCopy,
			IDbTransaction transaction = null,
			Action<SqlBulkCopy> sqlBulkCopy = null
		)
		{
			if (!columnsToCopy.Any())
				return string.Empty;

			if (connection.State != ConnectionState.Open)
				throw new ArgumentException("Connection is required to be opened by the calling code.");

			var insertedTableName = await connection.CreateTemporaryTableFromTable(tableName, columnsToCopy, transaction);

			if (columnsToCopy.Count() * entitiesToInsert.Count() < 2000 || !(connection is SqlConnection))
			{
				var maxParams = 2000M;
				var batchSize = Math.Min((int)Math.Floor(maxParams / columnsToCopy.Count()), 1000);

				await connection.ExecuteAsyncWithTransaction(
					async (conn, tran) =>
					{
						foreach (var batch in entitiesToInsert.Batch(batchSize))
						{
							var (valuesQuery, parameters) = columnsToCopy.ColumnListAsValueParamaters(batch);

							var query = $@"
								INSERT INTO {insertedTableName} ({columnsToCopy.ColumnList()})
								{valuesQuery}
							";

							await connection.ExecuteAsync(
								query.ToString(),
								parameters,
								transaction: tran
							);
						}

						return true;
					},
					transaction: transaction
				);
			}
			else
			{
				using (var bulkCopy = new SqlBulkCopy((SqlConnection)connection, SqlBulkCopyOptions.Default, (SqlTransaction)transaction))
				{
					sqlBulkCopy?.Invoke(bulkCopy);
					bulkCopy.DestinationTableName = insertedTableName;
					await bulkCopy.WriteToServerAsync(ToDataTable(entitiesToInsert, columnsToCopy).CreateDataReader());
				}
			}

			return insertedTableName;
		}

		public static DataTable ToDataTable<T>(IEnumerable<T> data, IEnumerable<PropertyInfo> properties)
		{
			var typeCasts = properties.Select(x =>
			{
				var nullableUnderlyingType = Nullable.GetUnderlyingType(x.PropertyType);

				bool isNullable = !x.PropertyType.IsValueType
				|| (x.PropertyType.IsGenericType && x.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
				|| nullableUnderlyingType != null;

				var type = nullableUnderlyingType ?? x.PropertyType;

				Type castType = null;
				if (type.IsEnum)
					castType = Enum.GetUnderlyingType(type);

				return new
				{
					x.Name,
					Nullable = isNullable,
					Type = castType ?? type,
					CastType = castType,
					Property = x
				};
			}
			).ToList();

			var dataTable = new DataTable();

			foreach (var item in typeCasts)
			{
				dataTable.Columns.Add(
					new DataColumn()
					{
						ColumnName = item.Name,
						AllowDBNull = item.Nullable,
						DataType = item.Type,
					}
				);
			}

			foreach (var item in data)
			{
				dataTable.Rows.Add(
					typeCasts.Select(x =>
					{
						var value = x.Property.GetValue(item, null);
						if (x.Nullable)
						{
							if (value == null)
								return DBNull.Value;

							if (x.CastType != null)
								return Convert.ChangeType(value, x.CastType);

							return Convert.ChangeType(value, x.Type);
						}
						else
						{
							if (x.CastType != null)
								return Convert.ChangeType(value, x.CastType);

							return value;
						}
					}
					).ToArray()
				);
			}

			return dataTable;
		}

		public async static Task<IEnumerable<T>> GetBulkAsync<T>(
			this IDbConnection connection,
			IEnumerable<T> entitiesToGet,
			IDbTransaction transaction = null,
			int? commandTimeout = null,
			Action<SqlBulkCopy> sqlBulkCopy = null
		)
		{
			if (!entitiesToGet.Any())
				return new List<T>();

			var typeMeta = TypeMeta.Get<T>();
			if (typeMeta.PropertiesKey.Count == 0 && typeMeta.PropertiesExplicit.Count == 0)
				throw new ArgumentException("Entity must have at least one [Key] or [ExplicitKey] property");

			var result = await connection.ExecuteAsync(
				entitiesToGet,
				typeMeta.PropertiesKeyAndExplicit,
				async (connection, source, parameters, properties) =>
				{
					var query = $@"
						SELECT *
						FROM
							{source} AS Source
							INNER JOIN {typeMeta.TableName} AS Target
								ON {properties.ColumnListEquals(" AND ")};
					";

					return await connection.QueryAsync<T>(
						query,
						param: parameters,
						commandTimeout: commandTimeout,
						transaction: transaction
					);
				},
				transaction: transaction,
				sqlBulkCopy: sqlBulkCopy
			);
			return result;
		}

		/// <summary>
		/// Inserts an entity into table "Ts" and returns identity id or number of inserted rows if inserting a list.
		/// </summary>
		/// <typeparam name="T">The type to insert.</typeparam>
		/// <param name="connection">Open SqlConnection</param>
		/// <param name="entitiesToInsert">Entity to insert, can be list of entities</param>
		/// <param name="transaction">The transaction to run under, null (the default) if none</param>
		/// <param name="commandTimeout">Number of seconds before command execution timeout</param>
		/// <param name="outputMap"></param>
		/// <returns>Identity of inserted entity, or number of inserted rows if inserting a list</returns>
		public async static Task<int> InsertBulkAsync<T>(
			this IDbConnection connection,
			IEnumerable<T> entitiesToInsert,
			IDbTransaction transaction = null,
			int? commandTimeout = null,
			Action<SqlBulkCopy> sqlBulkCopy = null,
			Action<IEnumerable<T>, IEnumerable<T>, IEnumerable<PropertyInfo>, IEnumerable<PropertyInfo>> outputMap = null
		) where T : class
		{
			var entityCount = entitiesToInsert.Count();
			if (entityCount == 0)
				return 0;

			var typeMeta = TypeMeta.Get<T>();

			var mapGeneratedValues = outputMap != null;
			if (mapGeneratedValues && !typeMeta.PropertiesKeyAndExplicit.Any())
				throw new NotSupportedException("Output mapping inserted values is not supported without either a key or explicitkey");

			return await connection.ExecuteAsync(
				entitiesToInsert,
				typeMeta.PropertiesExceptKeyAndComputed,
				async (conn, source, parameters, properties) =>
				{
					var columnList = properties.ColumnList();
					var query = $@"
						INSERT INTO {typeMeta.TableName} ({columnList})
						/**OUTPUT**/
						SELECT {columnList} FROM {source} AS Source;
					";

					var outputSource = source;
					var outputClause = string.Empty;
					if (mapGeneratedValues)
					{
						if (typeMeta.PropertiesKey.Any())
						{
							var outputTarget = await conn.CreateTemporaryTableFromTable(
								typeMeta.TableName,
								typeMeta.PropertiesKeyAndExplicit,
								transaction
							);

							outputSource = outputTarget;
							outputClause = OutputMapExtensions.OutputClause(outputTarget, typeMeta.PropertiesKeyAndExplicit);
						}
					}

					query = query.Replace("/**OUTPUT**/", outputClause);
					return await conn.ExecuteWithOutputMapAsync<T>(
						query,
						parameters,
						outputSource,
						mapGeneratedValues,
						(index, output) =>
						{
							outputMap(
								entitiesToInsert,
								output,
								typeMeta.PropertiesExceptKeyAndComputed,
								typeMeta.Properties
							);
						},
						transaction,
						commandTimeout
					);
				},
				transaction: transaction,
				sqlBulkCopy: sqlBulkCopy
			);
		}



		/// <summary>
		/// Updates entity in table "Ts", checks if the entity is modified if the entity is tracked by the Get() extension.
		/// </summary>
		/// <typeparam name="T">Type to be updated</typeparam>
		/// <param name="connection">Open SqlConnection</param>
		/// <param name="entitiesToUpdate">Entity to be updated</param>
		/// <param name="transaction">The transaction to run under, null (the default) if none</param>
		/// <param name="commandTimeout">Number of seconds before command execution timeout</param>
		/// <returns>true if updated, false if not found or not modified (tracked entities)</returns>
		public async static Task<int> UpdateBulkAsync<T>(
			this IDbConnection connection,
			IEnumerable<T> entitiesToUpdate,
			IDbTransaction transaction = null,
			int? commandTimeout = null,
			Action<SqlBulkCopy> sqlBulkCopy = null,
			Action<IEnumerable<T>, IEnumerable<T>, IEnumerable<PropertyInfo>, IEnumerable<PropertyInfo>> outputMap = null
		) where T : class
		{
			entitiesToUpdate = entitiesToUpdate.Where(x => x is SqlMapperExtensions.IProxy proxy && !proxy.IsDirty || !(x is SqlMapperExtensions.IProxy));

			var entityCount = entitiesToUpdate.Count();
			if (entityCount == 0)
				return 0;

			var mapGeneratedValues = outputMap != null;
			var typeMeta = TypeMeta.Get<T>();
			if (typeMeta.PropertiesKey.Count == 0 && typeMeta.PropertiesExplicit.Count == 0)
				throw new ArgumentException("Entity must have at least one [Key] or [ExplicitKey] property");

			return await connection.ExecuteAsync(
				entitiesToUpdate,
				typeMeta.PropertiesExceptComputed,
				async (conn, source, parameters, properties) =>
				{
					var query = $@"
						UPDATE Target
						SET
							{typeMeta.PropertiesExceptKeyComputedAndImmutable.ColumnListEquals(", ")}
						FROM
							{source} AS Source
							INNER JOIN {typeMeta.TableName} AS Target
								ON {typeMeta.PropertiesKeyAndExplicit.ColumnListEquals(" AND ")};
					";

					return await conn.ExecuteWithOutputMapAsync<T>(
						query,
						parameters,
						source,
						mapGeneratedValues,
						(index, output) =>
						{
							outputMap(
								entitiesToUpdate,
								output,
								typeMeta.PropertiesKeyAndExplicit,
								typeMeta.Properties
							);
						},
						transaction,
						commandTimeout
					);
				},
				transaction: transaction,
				sqlBulkCopy: sqlBulkCopy
			);
		}

		/// <summary>
		/// Updates entity in table "Ts", checks if the entity is modified if the entity is tracked by the Get() extension.
		/// </summary>
		/// <typeparam name="T">Type to be updated</typeparam>
		/// <param name="connection">Open SqlConnection</param>
		/// <param name="entitiesToDelete">Entity to be updated</param>
		/// <param name="transaction">The transaction to run under, null (the default) if none</param>
		/// <param name="commandTimeout">Number of seconds before command execution timeout</param>
		/// <returns>true if updated, false if not found or not modified (tracked entities)</returns>
		public async static Task<int> DeleteBulkAsync<T>(
			this IDbConnection connection,
			IEnumerable<T> entitiesToDelete,
			IDbTransaction transaction = null,
			int? commandTimeout = null,
			Action<SqlBulkCopy> sqlBulkCopy = null
		) where T : class
		{
			var entityCount = entitiesToDelete.Count();
			if (entityCount == 0)
				return 0;

			if (entityCount == 1)
				return await connection.DeleteAsync(entitiesToDelete.First(), transaction: transaction, commandTimeout: commandTimeout) ? 1 : 0;

			var typeMeta = TypeMeta.Get<T>();
			if (typeMeta.PropertiesKey.Count == 0 && typeMeta.PropertiesExplicit.Count == 0)
				throw new ArgumentException("Entity must have at least one [Key] or [ExplicitKey] property");

			var result = await connection.ExecuteAsync(
				entitiesToDelete,
				typeMeta.PropertiesKeyAndExplicit,
				async (connection, source, parameters, properties) =>
				{
					var query = $@"
						DELETE Target
						FROM
							{source} AS Source
							INNER JOIN {typeMeta.TableName} AS Target
								ON {properties.ColumnListEquals(" AND ")};
					";

					return await connection.ExecuteAsync(
						query,
						param: parameters,
						commandTimeout: commandTimeout,
						transaction: transaction
					);
				},
				transaction: transaction,
				sqlBulkCopy: sqlBulkCopy
			);
			return result;
		}

		public static async Task<(string source, DynamicParameters parameters)> BulkSourceAsync<T>(
			this IDbConnection connection,
			IEnumerable<T> entities,
			IEnumerable<PropertyInfo> properties,
			IDbTransaction transaction = null,
			Action<SqlBulkCopy> sqlBulkCopy = null
		)
		{
			var entityCount = entities.Count();

			if (entities == null)
				throw new ArgumentNullException(nameof(entities));

			if (properties == null)
				throw new ArgumentNullException(nameof(properties));

			if (entityCount == 0)
				throw new ArgumentOutOfRangeException(nameof(entities));

			if (entityCount * properties.Count() < 2000)
			{
				var (valueQuery, parameters) = properties.ColumnListAsValueParamaters(entities);
				var source = $@"
					(
						SELECT *
						FROM
						(
							{valueQuery}
						)
							AS SourceInner (
								{properties.ColumnList()}
							)
					)
				";

				return (source, parameters);
			}

			var typeMeta = TypeMeta.Get<T>();

			var insertedTableName = await connection.TransferBulkAsync(
				entities,
				typeMeta.TableName,
				properties,
				transaction: transaction,
				sqlBulkCopy: sqlBulkCopy
			);

			return (insertedTableName, null);
		}

		public static async Task<R> ExecuteAsync<T, R>(
			this IDbConnection connection,
			IEnumerable<T> entities,
			IEnumerable<PropertyInfo> properties,
			Func<IDbConnection, string, DynamicParameters, IEnumerable<PropertyInfo>, Task<R>> executor,
			IDbTransaction transaction = null,
			Action<SqlBulkCopy> sqlBulkCopy = null
		)
		{
			return await connection.ExecuteAsync(
				async (conn) =>
				{
					var (source, parameters) = await connection.BulkSourceAsync(
						entities,
						properties,
						transaction: transaction,
						sqlBulkCopy: sqlBulkCopy
					);

					return await executor(connection, source, parameters, properties);
				}
			);
		}
	}
}