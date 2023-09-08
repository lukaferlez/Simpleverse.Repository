using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Data.Common;
using System.Threading.Tasks;

namespace Simpleverse.Repository.Db.SqlServer
{
	public class SqlRepository : DbRepository
	{
		public SqlRepository(IConfiguration configuration, string connectionStringName)
			: this(configuration.GetConnectionString(connectionStringName))
		{
		}

		public SqlRepository(string connectionString)
			: base(() => new SqlConnection(connectionString))
		{
		}

		public async Task<R> ExecuteWithAppLockAsync<R>(string resourceIdentifier, Func<DbConnection, DbTransaction, Task<R>> function)
		{
			return await ExecuteAsyncWithTransaction(
				async (conn, tran) =>
				{
					return await ((SqlConnection)conn, (SqlTransaction)tran).ExecuteWithAppLockAsync(resourceIdentifier, function);
				}
			);
		}
	}
}
