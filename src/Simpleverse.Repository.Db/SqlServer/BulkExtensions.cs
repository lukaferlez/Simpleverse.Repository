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
using Simpleverse.Repository.Db.Meta;
using Simpleverse.Repository.Db.SqlServer;
using System.Data.Common;
using Simpleverse.Repository.Db.SqlServer.Merge;
using Microsoft.Extensions.Logging;
using System.IO.MemoryMappedFiles;

namespace Simpleverse.Repository.Db.SqlServer
{
	public static class BulkExtensions
	{
		public static async Task<string> TransferBulkAsync<T>(
			this DbConnection connection,
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
			this DbConnection connection,
			IEnumerable<T> entitiesToInsert,
			string tableName,
			IEnumerable<PropertyInfo> columnsToCopy,
			DbTransaction transaction = null,
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

			if (columnsToCopy.Count() * entitiesToInsert.Count() < 2000 || !(connection is SqlConnection))
			{
				var maxParams = 2000M;
				var batchSize = Math.Min((int)Math.Floor(maxParams / columnsToCopy.Count()), 1000);

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
						transaction: transaction
					);
				}
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

				var type = nullableUnderlyingType ?? x.PropertyType;

				Type castType = null;
				if (type.IsEnum)
					castType = Enum.GetUnderlyingType(type);

				return new
				{
					x.Name,
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
			this DbConnection connection,
			IEnumerable<T> entitiesToGet,
			DbTransaction transaction = null,
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
				async (connection, transaction, source, parameters, properties) =>
				{
					var query = $@"
						SELECT *
						FROM
							{source} AS Source
							INNER JOIN {typeMeta.TableName} AS Target
								ON {properties.ColumnListEquals(" AND ")};
					";

					return await connection.QueryAsync<T>(query, param: parameters, commandTimeout: commandTimeout, transaction: transaction);
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
			this DbConnection connection,
			IEnumerable<T> entitiesToInsert,
			DbTransaction transaction = null,
			int? commandTimeout = null,
			Action<SqlBulkCopy> sqlBulkCopy = null,
			Action<IEnumerable<T>, IEnumerable<T>, IEnumerable<PropertyInfo>, IEnumerable<PropertyInfo>> outputMap = null
		) where T : class
		{
			var entityCount = entitiesToInsert.Count();
			if (entityCount == 0)
				return 0;

			var mapGeneratedValues = outputMap != null;
			var typeMeta = TypeMeta.Get<T>();

			var outputEntities = new List<T>();
			var result =
				await connection.ExecuteAsync(
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
							var results = await connection.QueryAsync<T>(
								query,
								param: parameters,
								commandTimeout: commandTimeout,
								transaction: transaction
							);
							outputEntities.AddRange(results);
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
				)
			;

			if (mapGeneratedValues)
				outputMap(
					entitiesToInsert,
					outputEntities,
					typeMeta.PropertiesExceptKeyAndComputed,
					typeMeta.PropertiesKeyAndComputed
				);

			return result;
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
			this DbConnection connection,
			IEnumerable<T> entitiesToUpdate,
			DbTransaction transaction = null,
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

			var outputValues = new List<T>();
			var result =
				await connection.ExecuteAsync(
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
				)
			;

			if (mapGeneratedValues)
				outputMap(
					entitiesToUpdate,
					outputValues,
					typeMeta.PropertiesKeyAndExplicit,
					typeMeta.PropertiesComputed
				);

			return result;
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
			this DbConnection connection,
			IEnumerable<T> entitiesToDelete,
			DbTransaction transaction = null,
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
				async (connection, transaction, source, parameters, properties) =>
				{
					var query = $@"
						DELETE Target
						FROM
							{source} AS Source
							INNER JOIN {typeMeta.TableName} AS Target
								ON {properties.ColumnListEquals(" AND ")};
					";

					return await connection.ExecuteAsync(query, param: parameters, commandTimeout: commandTimeout, transaction: transaction);
				},
				transaction: transaction,
				sqlBulkCopy: sqlBulkCopy
			);
			return result;
		}

		private static string OutputClause(IEnumerable<PropertyInfo> properties = null)
		{
			var keyColumns = properties?.ColumnList(prefix: "inserted");
			if (string.IsNullOrWhiteSpace(keyColumns))
				return "OUTPUT inserted.*";

			return "OUTPUT " + keyColumns;
		}

		public static async Task<(string source, DynamicParameters parameters)> BulkSourceAsync<T>(
			this DbConnection connection,
			IEnumerable<T> entities,
			IEnumerable<PropertyInfo> properties,
			DbTransaction transaction = null,
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

		public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> items, int size)
		{
			var batch = new List<T>();
			var index = 0;
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

		public static async Task<R> ExecuteAsync<T, R>(
			this DbConnection connection,
			IEnumerable<T> entities,
			IEnumerable<PropertyInfo> properties,
			Func<DbConnection, DbTransaction, string, DynamicParameters, IEnumerable<PropertyInfo>, Task<R>> executor,
			DbTransaction transaction = null,
			Action<SqlBulkCopy> sqlBulkCopy = null
		)
		{
			var connectonWasClosed = connection.State == ConnectionState.Closed;
			if (connectonWasClosed)
				connection.Open();

			var transactionWasClosed = transaction == null;
			if (transactionWasClosed)
				transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

			var result = default(R);
			try
			{
				var (source, parameters) = await connection.BulkSourceAsync(
					entities,
					properties,
					transaction: transaction,
					sqlBulkCopy: sqlBulkCopy
				);

				result = await executor(connection, transaction, source, parameters, properties);
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