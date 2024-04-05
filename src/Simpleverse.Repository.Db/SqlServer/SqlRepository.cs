﻿using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using StackExchange.Profiling.Data;
using System;
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

	public interface ISqlRepository : IDbRepository
	{

	}
}
