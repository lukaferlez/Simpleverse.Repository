using Dapper;
using Microsoft.Data.SqlClient;
using System;
using System.Threading.Tasks;

namespace Simpleverse.Repository.Db.SqlServer
{
	public static class SqlConnectionExtensions
	{
		public static async Task<R> ExecuteAsyncWithTransaction<R>(this SqlConnection conn, Func<SqlConnection, SqlTransaction, Task<R>> function)
		{
			using (var tran = conn.BeginTransaction())
			{
				var result = await function(conn, tran);
				await tran.CommitAsync();
				return result;
			}
		}

		#region AppLock

		public static async Task<bool> GetAppLockAsync(this SqlConnection connection, string key, SqlTransaction transaction = null)
		{
			if (key.Length > 255)
				throw new ArgumentOutOfRangeException(nameof(key), "ength of the key used for locking must be less then 256 characters.");

			var result = await connection.ExecuteScalarAsync<int>(
				@"
					declare @result int
					exec @result = sp_getapplock @Resource, @LockMode
					select @result
				",
				new { Resource = key, LockMode = "Exclusive" },
				transaction: transaction
			);

			return result == 0 || result == 1;
		}

		public static async Task<bool> ReleaseAppLockAsync(this SqlConnection connection, string key, SqlTransaction transaction = null)
		{
			if (key.Length > 255)
				throw new ArgumentOutOfRangeException(nameof(key), "Length of the key used for locking must be less then 256 characters.");

			var result = await connection.ExecuteScalarAsync<int>(
				@"
					declare @result int
					exec @result = sp_releaseapplock @Resource
					select @result
				",
				new { Resource = key },
				transaction: transaction
			);

			return result == 0;
		}

		public static Task<R> ExecuteWithAppLockAsync<R>(
			this SqlConnection conn,
			string resourceIdentifier,
			Func<SqlConnection, SqlTransaction, Task<R>> function
		)
		{
			return conn.ExecuteAsyncWithTransaction(
				(conn, tran) => (conn, tran).ExecuteWithAppLockAsync(resourceIdentifier, function)
			);
		}

		public static async Task<R> ExecuteWithAppLockAsync<R>(
			this (SqlConnection conn, SqlTransaction tran) context,
			string resourceIdentifier,
			Func<SqlConnection, SqlTransaction, Task<R>> function
		)
		{
			var lockResult = await context.conn.GetAppLockAsync(resourceIdentifier, transaction: context.tran);
			if (!lockResult)
				throw new Exception($"Could not obtain lock for {resourceIdentifier}");

			var result = await function(context.conn, context.tran);
			await context.conn.ReleaseAppLockAsync(resourceIdentifier, transaction: context.tran);
			return result;
		}

		#endregion
	}
}
