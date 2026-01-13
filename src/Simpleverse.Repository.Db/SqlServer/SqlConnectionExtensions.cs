using Dapper;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Simpleverse.Repository.Db.SqlServer
{
	public static class SqlConnectionExtensions
	{
		public async static Task<string> CreateTemporaryTableFromTable(
			this IDbConnection connection,
			string tableName,
			IEnumerable<PropertyInfo> columns,
			IDbTransaction transaction,
			IEnumerable<string> arbitraryColumns = null
		)
		{
			var insertedTableName = $"#tbl_{Guid.NewGuid().ToString().Replace("-", string.Empty)}";

			var columnsString = columns.ColumnList();
			if (arbitraryColumns != null)
			{
				columnsString += " ,";
				columnsString += string.Join(", ", arbitraryColumns);
			}

			await connection.ExecuteAsync(
				$@"SELECT TOP 0 {columnsString} INTO {insertedTableName} FROM {tableName} WITH(NOLOCK)
				UNION ALL
				SELECT TOP 0 {columnsString} FROM {tableName} WITH(NOLOCK);
				",
				transaction: transaction
			);
			return insertedTableName;
		}

		public static async Task<R> ExecuteAsyncWithTransaction<R>(
			this SqlConnection conn,
			Func<SqlConnection, IDbTransaction, Task<R>> function
		)
		{
			using (var tran = await conn.BeginTransactionAsync())
			{
				var result = await function(conn, tran);
				await tran.CommitAsync();
				return result;
			}
		}

		#region AppLock

		public static async Task<bool> GetAppLockAsync(this SqlConnection connection, string key, IDbTransaction transaction = null, TimeSpan? lockTimeout = null)
		{
			if (key.Length > 255)
				throw new ArgumentOutOfRangeException(nameof(key), "Length of the key used for locking must be less then 256 characters.");

			var result = await connection.ExecuteScalarAsync<int>(
				@"
					declare @result int
					exec @result = sp_getapplock @Resource, @LockMode, @LockTimeout = @Timeout;
					select @result
				",
				new { Resource = key, LockMode = "Exclusive", Timeout = lockTimeout == null ? -1 : lockTimeout.Value.TotalMilliseconds },
				transaction: transaction
			);

			return result == 0 || result == 1;
		}

		public static async Task<bool> ReleaseAppLockAsync(this SqlConnection connection, string key, IDbTransaction transaction = null)
		{
			if (key.Length > 255)
				throw new ArgumentOutOfRangeException(nameof(key), "Length of the key used for locking must be less then 256 characters.");

			var lockOwner = transaction == null ? "Session" : "Transaction";

			var result = await connection.ExecuteScalarAsync<int>(
				@"
					declare @result int
					exec @result = sp_releaseapplock @Resource, @LockOwner
					select @result
				",
				new { Resource = key, LockOwner = lockOwner },
				transaction: transaction
			);

			return result == 0;
		}

		public static async Task<bool> ReleaseAppLockAsync(this SqlConnection connection, IEnumerable<string> keys, IDbTransaction transaction = null)
		{
			bool allReleased = true;

			foreach (var key in keys)
			{
				var released = await connection.ReleaseAppLockAsync(key, transaction);
				if (!released)
					allReleased = false;
			}

			return allReleased;
		}

		public static async Task<bool> TryGetAppLockAsync(this SqlConnection connection, IEnumerable<string> keys, int retryTimeout = 100, int numberOfRetries = 3, IDbTransaction transaction = null, TimeSpan? lockTimeout = null)
		{
			if (keys == null || !keys.Any())
				throw new ArgumentException("Keys collection must not be null or empty.", nameof(keys));
			if (numberOfRetries < 1)
				throw new ArgumentOutOfRangeException(nameof(numberOfRetries), "Number of retries must be at least 1.");
			if (retryTimeout < 0)
				throw new ArgumentOutOfRangeException(nameof(retryTimeout), "Retry timeout must be non-negative.");

			bool allLocked = true;

			for (int retry = 0; retry < numberOfRetries; retry++)
			{
				int lastAttemptedIndex = -1;
				allLocked = true;

				foreach (var key in keys)
				{
					var locked = await connection.GetAppLockAsync(key, transaction, lockTimeout);
					
					if (!locked)
					{
						allLocked = false;
						break;						
					}

					lastAttemptedIndex++;
				}

				if (allLocked)
					break;

				await connection.ReleaseAppLockAsync(keys.Take(lastAttemptedIndex + 1), transaction);
				await Task.Delay(retryTimeout);
			}

			return allLocked;
		}

		public static Task<R> ExecuteWithAppLockAsync<R>(
			this SqlConnection conn,
			string resourceIdentifier,
			Func<SqlConnection, IDbTransaction, Task<R>> function,
			TimeSpan? lockTimeout = null
		)
		{
			return conn.ExecuteAsyncWithTransaction(
				(conn, tran) => (conn, tran).ExecuteWithAppLockAsync(resourceIdentifier, function, lockTimeout: lockTimeout)
			);
		}

		public static async Task<R> ExecuteWithAppLockAsync<R>(
			this (SqlConnection conn, IDbTransaction tran) context,
			string resourceIdentifier,
			Func<SqlConnection, IDbTransaction, Task<R>> function,
			TimeSpan? lockTimeout = null
		)
		{
			var lockResult = await context.conn.GetAppLockAsync(resourceIdentifier, transaction: context.tran, lockTimeout: lockTimeout);
			if (!lockResult)
				throw new Exception($"Could not obtain lock for {resourceIdentifier}");

			var result = await function(context.conn, context.tran);
			await context.conn.ReleaseAppLockAsync(resourceIdentifier, transaction: context.tran);
			return result;
		}

		#endregion
	}
}
