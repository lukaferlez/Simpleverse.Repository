using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Data.Common;
using System.IO.Hashing;
using System.Text;
using System.Threading.Tasks;

namespace Simpleverse.Repository.Db.SqlServer
{
	public class ApplicationLockScopeFactory
	{
		private readonly string connectionString;

		public ApplicationLockScopeFactory(string connectionString)
		{
			this.connectionString = connectionString;
		}

		public ApplicationLockScope Instance()
		{
			return new ApplicationLockScope(new SqlConnection(connectionString));
		}
	}

	public class ApplicationLockScope : IDisposable
	{
		private bool disposedValue;

		private bool wasClosed;
		private readonly SqlConnection connection;
		private DbTransaction transaction;

		public ApplicationLockScope(SqlConnection connection)
		{
			this.connection = connection;
		}

		public async Task<bool> LockAsync(params object[] keyItems)
		{
			var keyBytes = Encoding.UTF8.GetBytes(string.Concat(keyItems));
			var key = Convert.ToBase64String(XxHash64.Hash(keyBytes));

			if (transaction == null)
			{
				wasClosed = connection.State == ConnectionState.Closed;
				if (wasClosed)
					await connection.OpenAsync();

				transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted);
			}

			return await connection.GetAppLockAsync(key);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					if (wasClosed)
						connection.Close();

					transaction.Dispose();
					transaction = null;
				}
				disposedValue = true;
			}
		}

		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}

	public class ApplicationLock
	{
		private readonly string connectionString;

		public ApplicationLock(string connectionString)
		{
			this.connectionString = connectionString;
		}

		public T Execute<T>(object key, Func<object, T> factory)
		{
			var task = ExecuteAsync(
				key,
				x =>
				{
					return Task.FromResult(factory(x));
				}
			);
			task.Wait();
			return task.Result;
		}

		public async Task<T> ExecuteAsync<T>(object key, Func<object, Task<T>> factory)
		{
			using (var connection = new SqlConnection(connectionString))
			{
				await connection.OpenAsync();
				return await connection.ExecuteWithAppLockAsync(
					key.ToString(),
					(connection, key) => factory(key)
				);
			}
		}
	}
}
