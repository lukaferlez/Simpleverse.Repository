using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;
using Dapper.Contrib.Extensions;
using System.Dynamic;
using System.Xml;
using System.IO.MemoryMappedFiles;

namespace Simpleverse.Dapper.SqlServer
{
	public static class BulkExtensions
	{
		public static async Task<string> TransferBulkAsync<T>(
			this SqlConnection connection,
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
			this SqlConnection connection,
			IEnumerable<T> entitiesToInsert,
			string tableName,
			IEnumerable<PropertyInfo> columnsToCopy,
			SqlTransaction transaction = null,
			Action<SqlBulkCopy> sqlBulkCopy = null
		)
		{
			if (!columnsToCopy.Any())
				return string.Empty;

			var insertedTableName = $"#tbl_{Guid.NewGuid().ToString().Replace("-", string.Empty)}";

			if (connection.State != ConnectionState.Open)
				throw new ArgumentException("Connection is required to be opened by the calling code.");

			connection.Execute(
				$@"SELECT TOP 0 {columnsToCopy.ColumnList()} INTO {insertedTableName} FROM {tableName} WITH(NOLOCK)
				UNION ALL
				SELECT TOP 0 {columnsToCopy.ColumnList()} FROM {tableName} WITH(NOLOCK);
				"
				, null
				, transaction
			);

			if (columnsToCopy.Count() * entitiesToInsert.Count() < 2000)
			{
				var (valuesQuery, parameters) = columnsToCopy.ColumnListAsValueParamaters(entitiesToInsert);

				var query = $@"
					INSERT INTO {insertedTableName} ({columnsToCopy.ColumnList()})
					{valuesQuery}
				";

				await connection.ExecuteAsync(
					query.ToString(),
					parameters,
					transaction: transaction
				);
			}
			else
			{
				using (var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction))
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

					var type = nullableUnderlyingType ?? x.PropertyType;

					Type castType = null;
					if (type.IsEnum)
						castType = Enum.GetUnderlyingType(type);

					return new
					{
						Name = x.Name,
						Nullable = nullableUnderlyingType != null,
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
			this SqlConnection connection,
			IEnumerable<T> entitiesToGet,
			SqlTransaction transaction = null,
			int? commandTimeout = null,
			Action<SqlBulkCopy> sqlBulkCopy = null
		)
		{
			if (!entitiesToGet.Any())
				return new List<T>();

			var typeMeta = TypeMeta.Get<T>();
			if (typeMeta.PropertiesKey.Count == 0 && typeMeta.PropertiesExplicit.Count == 0)
				throw new ArgumentException("Entity must have at least one [Key] or [ExplicitKey] property");

			var result = await connection.Execute(
				entitiesToGet,
				typeMeta.PropertiesKeyAndExplicit,
				async (connection, transaction, source, parameters, properties) =>
				{
					var query = $@"
						SELECT *
						FROM
							{ source } AS Source
							INNER JOIN { typeMeta.TableName } AS Target
								ON { properties.ColumnListEquals(" AND ") };
					";

					return await connection.QueryAsync<T>(query, param: parameters, commandTimeout: commandTimeout, transaction: transaction);
				},
				transaction: transaction
			);
			return result.SelectMany(x => x.Select(y => y));
		}

		/// <summary>
		/// Inserts an entity into table "Ts" and returns identity id or number of inserted rows if inserting a list.
		/// </summary>
		/// <typeparam name="T">The type to insert.</typeparam>
		/// <param name="connection">Open SqlConnection</param>
		/// <param name="entitiesToInsert">Entity to insert, can be list of entities</param>
		/// <param name="transaction">The transaction to run under, null (the default) if none</param>
		/// <param name="commandTimeout">Number of seconds before command execution timeout</param>
		/// <returns>Identity of inserted entity, or number of inserted rows if inserting a list</returns>
		public async static Task<int> InsertBulkAsync<T>(
			this SqlConnection connection,
			IEnumerable<T> entitiesToInsert,
			SqlTransaction transaction = null,
			int? commandTimeout = null,
			Action<SqlBulkCopy> sqlBulkCopy = null,
			bool mapGeneratedValues = false
		) where T : class
		{
			var entityCount = entitiesToInsert.Count();
			if (entityCount == 0)
				return 0;

			var typeMeta = TypeMeta.Get<T>();

			var propertiesKeyAndComputed = typeMeta.PropertiesKey.Union(typeMeta.PropertiesComputed);

			var outputValues = new List<dynamic>();
			var result = (
				await connection.Execute(
					entitiesToInsert,
					typeMeta.PropertiesExceptKeyAndComputed,
					async (connection, transaction, source, parameters, properties) =>
					{
						var columnList = properties.ColumnList();

						var query = $@"
							INSERT INTO {typeMeta.TableName} ({columnList}) 
							{(mapGeneratedValues ? OutputClause() : string.Empty)}
							SELECT {columnList} FROM {source} AS Source;
						";

						if (mapGeneratedValues)
						{
							var results = await connection.QueryAsync(
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
					commandTimeout: commandTimeout,
					sqlBulkCopy: sqlBulkCopy
				)
			);

			if (mapGeneratedValues)
				MapGeneratedValues(
					entitiesToInsert,
					outputValues,
					typeMeta.PropertiesExceptKeyAndComputed, propertiesKeyAndComputed,
					true
				);

			return result.Sum();
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
			this SqlConnection connection,
			IEnumerable<T> entitiesToUpdate,
			SqlTransaction transaction = null,
			int? commandTimeout = null,
			Action<SqlBulkCopy> sqlBulkCopy = null,
			bool mapGeneratedValues = false
		) where T : class
		{
			entitiesToUpdate = entitiesToUpdate.Where(x => (x is SqlMapperExtensions.IProxy proxy && !proxy.IsDirty) || !(x is SqlMapperExtensions.IProxy));

			var entityCount = entitiesToUpdate.Count();
			if (entityCount == 0)
				return 0;

			var typeMeta = TypeMeta.Get<T>();
			if (typeMeta.PropertiesKey.Count == 0 && typeMeta.PropertiesExplicit.Count == 0)
				throw new ArgumentException("Entity must have at least one [Key] or [ExplicitKey] property");

			var outputValues = new List<dynamic>();
			var result = (
				await connection.Execute(
					entitiesToUpdate,
					typeMeta.PropertiesExceptComputed,
					async (connection, transaction, source, parameters, properties) =>
					{
						var query = $@"
							UPDATE Target
							SET
								{typeMeta.PropertiesExceptKeyAndComputed.ColumnListEquals(", ")}
							{(mapGeneratedValues ? OutputClause() : string.Empty)}
							FROM
								{source} AS Source
								INNER JOIN {typeMeta.TableName} AS Target
									ON {typeMeta.PropertiesKeyAndExplicit.ColumnListEquals(" AND ")};
						";

						if (mapGeneratedValues)
						{
							var results = await connection.QueryAsync(
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
					commandTimeout: commandTimeout,
					sqlBulkCopy: sqlBulkCopy
				)
			);

			if (mapGeneratedValues)
				MapGeneratedValues(
					entitiesToUpdate,
					outputValues,
					typeMeta.PropertiesKeyAndExplicit,
					typeMeta.PropertiesComputed,
					false
				);

			return result.Sum();
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
			this SqlConnection connection,
			IEnumerable<T> entitiesToDelete,
			SqlTransaction transaction = null,
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

			var result = await connection.Execute(
				entitiesToDelete,
				typeMeta.PropertiesKeyAndExplicit,
				async (connection, transaction, source, parameters, properties) =>
				{
					var query = $@"
						DELETE Target
						FROM
							{ source } AS Source
							INNER JOIN { typeMeta.TableName } AS Target
								ON { properties.ColumnListEquals(" AND ") };
					";

					return await connection.ExecuteAsync(query, param: parameters, commandTimeout: commandTimeout, transaction: transaction);
				},
				transaction: transaction,
					commandTimeout: commandTimeout,
					sqlBulkCopy: sqlBulkCopy
			);
			return result.Sum();
		}

		private static string OutputClause(IEnumerable<PropertyInfo> properties = null)
		{
			var keyColumns = properties?.ColumnList(prefix: "inserted");
			if (string.IsNullOrWhiteSpace(keyColumns))
				return "OUTPUT inserted.*";

			return "OUTPUT " + keyColumns;
		}

		private static void MapGeneratedValues<T>(
			IEnumerable<T> entities,
			IEnumerable<dynamic> results,
			IEnumerable<PropertyInfo> matchProperties,
			IEnumerable<PropertyInfo> propertiesToMap,
			bool mapResultOnce)
		{
			if (results == null || !results.Any())
				return;

			var entitiesWithMapping = entities
				.Select(x =>
					{ 
						dynamic entity = new ExpandoObject();
						entity.Entity = x;
						entity.Mapped = false;
						return entity;
					}
				)
				.ToList();

			foreach (IDictionary<string, object> result in results)
			{
				if (result.Count() == 0)
					continue;

				for (int iCount = 0; iCount < entitiesWithMapping.Count(); iCount++)
				{
					var entity = entitiesWithMapping[iCount];
					if (entity.Mapped)
						continue;

					var found = true;
					foreach (var property in matchProperties)
					{
						var entityValue = property.GetValue(entity.Entity);
						var resultValue = result[property.Name];

						if (!(
							(entityValue == null && resultValue == null) ||
							(entityValue != null && entityValue.Equals(resultValue)) ||
							(resultValue != null && resultValue.Equals(entityValue))
						))
						{
							found = false;
							break;
						}
					}

					if (found)
					{
						foreach (var keyProperty in propertiesToMap)
						{
							keyProperty.SetValue(entity.Entity, result[keyProperty.Name]);
						}
						entity.Mapped = true;

						if (mapResultOnce)
							break;
					}
				}
			}
		}

		public static async IAsyncEnumerable<(string source, DynamicParameters parameters)> BulkSourceAsync<T>(
			this SqlConnection connection,
			IEnumerable<T> entities,
			IEnumerable<PropertyInfo> properties,
			SqlTransaction transaction = null,
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

			if (entityCount * properties.Count() > 2000)
			{
				var typeMeta = TypeMeta.Get<T>();

				var insertedTableName = await connection.TransferBulkAsync(
					entities,
					typeMeta.TableName,
					properties,
					transaction: transaction,
					sqlBulkCopy: sqlBulkCopy
				);

				yield return (insertedTableName, null);
				yield break;
			}

			var maxParams = 2000M;
			var batchSize = (int) Math.Floor(maxParams / properties.Count());

			foreach(var batch in entities.Batch(batchSize))
			{
				var (valueQuery, parameters) = properties.ColumnListAsValueParamaters(batch);
				var source = $@"
					(
						SELECT *
						FROM
						(
							{valueQuery}
						)
							AS SourceInner (
								{ properties.ColumnList() }
							)
					)
				";

				yield return (source, parameters);
			}

			yield break;
		}

		public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> items, int size)
		{
			List<T> batch = new List<T>();
			int index = 0;
			foreach (var item in items)
			{
				batch.Add(item);
				index++;

				if (index % size == 0)
				{
					yield return batch;
					batch = new List<T>();
				}
			}

			if (batch.Count > 0)
				yield return batch;
		}

		public static async Task<IEnumerable<R>> Execute<T, R>(
			this SqlConnection connection,
			IEnumerable<T> entities,
			IEnumerable<PropertyInfo> properties,
			Func<SqlConnection, SqlTransaction, string, DynamicParameters, IEnumerable<PropertyInfo>, Task<R>> executor,
			SqlTransaction transaction = null,
			int? commandTimeout = null,
			Action<SqlBulkCopy> sqlBulkCopy = null
		)
		{
			var connectonWasClosed = connection.State == ConnectionState.Closed;
			if (connectonWasClosed)
				connection.Open();

			var transactionWasClosed = transaction == null;
			if (transactionWasClosed)
				transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

			var result = new List<R>();
			try
			{
				await foreach (var (source, parameters) in connection.BulkSourceAsync(
								entities,
								properties,
								transaction: transaction,
								sqlBulkCopy: sqlBulkCopy
							)
						)
				{
					result.Add(await executor(connection, transaction, source, parameters, properties));
				}
			}
			finally
			{
				if (transactionWasClosed)
				{
					transaction.Commit();
					transaction.Dispose();
				}

				if (connectonWasClosed)
					connection.Close();
			}

			return result;
		}
	}
}
