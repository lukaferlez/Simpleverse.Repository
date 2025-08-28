using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using StackExchange.Profiling.Data;
using System;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace Simpleverse.Repository.Db.SqlServer
{
	public class SqlRepository : DbRepository, ISqlRepository
	{
		public SqlRepository(IConfiguration configuration, string connectionStringName)
			: this(configuration.GetConnectionString(connectionStringName))
		{
		}

		public SqlRepository(string connectionString)
			: this(() => new SqlConnection(connectionString))
		{
		}

		public SqlRepository(Func<SqlConnection> connectionFactory)
			: base(connectionFactory)
		{

		}

		public SqlRepository(Func<DbConnection> connectionFactory)
			: base(connectionFactory)
		{

		}

		public SqlRepository(Func<ProfiledDbConnection> connectionFactory)
			: base(connectionFactory) { }

		public async Task<R> ExecuteWithAppLockAsync<R>(
			string resourceIdentifier,
			Func<DbConnection, IDbTransaction, Task<R>> function,
			TimeSpan? lockTimeout = null
		)
		{
			return await ExecuteAsyncWithTransaction(
				async (conn, tran) =>
				{
					return await ((SqlConnection)conn, tran).ExecuteWithAppLockAsync(resourceIdentifier, function, lockTimeout: lockTimeout);
				}
			);
		}
	}

	public interface ISqlRepository : IDbRepository
	{

	}
}
