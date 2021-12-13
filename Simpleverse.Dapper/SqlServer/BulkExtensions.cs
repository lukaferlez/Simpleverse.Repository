using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;
using Dapper.Contrib.Extensions;
using System.Text;

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
				var parameters = new DynamicParameters();

				var query = new StringBuilder($"INSERT INTO {insertedTableName} ({columnsToCopy.ColumnList()}) VALUES");

				int index = 0;
				foreach(var entity in entitiesToInsert)
				{
					var parameterList = columnsToCopy.ParameterList($"_{index}");
					if (index > 0)
						query.AppendLine(",");
					query.Append($"({parameterList})");

					foreach(var column in columnsToCopy)
					{
						parameters.Add(column.ParameterName($"_{index}"), column.GetValue(entity));
					}

					index++;
				}

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
			var typeMeta = TypeMeta.Get<T>();
			if (typeMeta.PropertiesKey.Count == 0 && typeMeta.PropertiesExplicit.Count == 0)
				throw new ArgumentException("Entity must have at least one [Key] or [ExplicitKey] property");

			var wasClosed = connection.State == ConnectionState.Closed;
			if (wasClosed) connection.Open();

			var insertedTableName = await connection.TransferBulkAsync(
				entitiesToGet,
				typeMeta.TableName,
				typeMeta.PropertiesKeyAndExplicit,
				transaction: transaction,
				sqlBulkCopy: sqlBulkCopy
			);

			var query = $@"
				SELECT *
				FROM
					{ insertedTableName } AS Source
					INNER JOIN { typeMeta.TableName } AS Target
						ON { typeMeta.PropertiesKeyAndExplicit.ColumnListEquals(" AND ") };
			";

			var enities = await connection.QueryAsync<T>(query, commandTimeout: commandTimeout, transaction: transaction);

			if (wasClosed) connection.Close();

			return enities;
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
			Action<SqlBulkCopy> sqlBulkCopy = null
			) where T : class
		{
			var entityCount = entitiesToInsert.Count();
			if (entityCount == 0)
				return 0;

			if (entityCount == 1)
				return await connection.InsertAsync(entitiesToInsert, transaction: transaction, commandTimeout: commandTimeout);

			var meta = TypeMeta.Get<T>();

			var wasClosed = connection.State == ConnectionState.Closed;
			if (wasClosed) connection.Open();

			var insertedTableName = await connection.TransferBulkAsync(entitiesToInsert, transaction: transaction, sqlBulkCopy: sqlBulkCopy);

			var columnList = meta.PropertiesExceptKeyAndComputed.ColumnList();

			var query = $@"
                INSERT INTO {meta.TableName} ({columnList}) 
                SELECT {columnList} FROM {insertedTableName};
			";

			var inserted = await connection.ExecuteAsync(query, commandTimeout, transaction);

			if (wasClosed) connection.Close();

			return inserted;
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
			Action<SqlBulkCopy> sqlBulkCopy = null
		) where T : class
		{
			entitiesToUpdate = entitiesToUpdate.Where(x => (x is SqlMapperExtensions.IProxy proxy && !proxy.IsDirty) || !(x is SqlMapperExtensions.IProxy));

			var entityCount = entitiesToUpdate.Count();
			if (entityCount == 0)
				return 0;

			if (entityCount == 1)
				return await connection.UpdateAsync(entitiesToUpdate.First(), transaction: transaction, commandTimeout: commandTimeout) ? 1 : 0;

			var typeMeta = TypeMeta.Get<T>();
			if (typeMeta.PropertiesKey.Count == 0 && typeMeta.PropertiesExplicit.Count == 0)
				throw new ArgumentException("Entity must have at least one [Key] or [ExplicitKey] property");

			var wasClosed = connection.State == ConnectionState.Closed;
			if (wasClosed) connection.Open();

			var insertedTableName = await connection.TransferBulkAsync(entitiesToUpdate, transaction: transaction, sqlBulkCopy: sqlBulkCopy);

			var query = $@"
				UPDATE Target
				SET
					{ typeMeta.PropertiesExceptKeyAndComputed.ColumnListEquals(", ") }
				FROM
					{ insertedTableName } AS Source
					INNER JOIN { typeMeta.TableName } AS Target
						ON { typeMeta.PropertiesKeyAndExplicit.ColumnListEquals(" AND ") };
			";

			var updated = await connection.ExecuteAsync(query, commandTimeout: commandTimeout, transaction: transaction);

			if (wasClosed) connection.Close();

			return updated;
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

			var wasClosed = connection.State == ConnectionState.Closed;
			if (wasClosed) connection.Open();

			var insertedTableName = await connection.TransferBulkAsync(
				entitiesToDelete,
				typeMeta.TableName,
				typeMeta.PropertiesKeyAndExplicit,
				transaction: transaction,
				sqlBulkCopy: sqlBulkCopy
			);

			var query = $@"
				DELETE Target
				FROM
					{ insertedTableName } AS Source
					INNER JOIN { typeMeta.TableName } AS Target
						ON { typeMeta.PropertiesKeyAndExplicit.ColumnListEquals(" AND ") };
			";

			var deleted = await connection.ExecuteAsync(query, commandTimeout: commandTimeout, transaction: transaction);

			if (wasClosed) connection.Close();

			return deleted;
		}
	}
}
