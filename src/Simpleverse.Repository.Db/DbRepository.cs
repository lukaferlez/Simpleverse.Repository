using Dapper;
using Simpleverse.Repository.Db.Meta;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;

namespace Simpleverse.Repository.Db
{
	public class DbRepository
	{
		private readonly Func<DbConnection> _connectionFactory;

		public DbConnection Connection => _connectionFactory();

		public DbRepository(Func<DbConnection> connectionFactory)
		{
			_connectionFactory = connectionFactory;
		}

		public Task<IEnumerable<R>> QueryAsync<R>(SqlBuilder.Template query)
			=> QueryAsync<R>(query.RawSql, query.Parameters);

		public Task<IEnumerable<dynamic>> QueryAsync(SqlBuilder.Template query)
			=> QueryAsync(query.RawSql, query.Parameters);

		public Task<IEnumerable<(TFirst, TSecond)>> QueryAsync<TFirst, TSecond>(SqlBuilder.Template query)
			=> QueryAsync<TFirst, TSecond>(query.RawSql, query.Parameters);

		public Task<IEnumerable<(TFirst, TSecond, TThrid)>> QueryAsync<TFirst, TSecond, TThrid>(SqlBuilder.Template query)
			=> QueryAsync<TFirst, TSecond, TThrid>(query.RawSql, query.Parameters);

		public async Task<IEnumerable<R>> QueryAsync<R>(string rawSql, object parameters)
		{
			return await ExecuteAsync((conn, tran) => conn.QueryAsync<R>(rawSql, param: parameters, transaction: tran));
		}

		public async Task<IEnumerable<dynamic>> QueryAsync(string rawSql, object parameters)
		{
			return await ExecuteAsync((conn, tran) => conn.QueryAsync(rawSql, param: parameters, transaction: tran));
		}

		public async Task<IEnumerable<(TFirst, TSecond)>> QueryAsync<TFirst, TSecond>(string rawSql, object parameters)
		{
			return await ExecuteAsync((conn, tran) => conn.QueryAsync<TFirst, TSecond, (TFirst, TSecond)>(rawSql, (first, second) => (first, second), param: parameters, transaction: tran));
		}

		public async Task<IEnumerable<(TFirst, TSecond, TThird)>> QueryAsync<TFirst, TSecond, TThird>(string rawSql, object parameters)
		{
			return await ExecuteAsync((conn, tran) => conn.QueryAsync<TFirst, TSecond, TThird, (TFirst, TSecond, TThird)>(rawSql, (first, second, third) => (first, second, third), param: parameters, transaction: tran));
		}

		public async Task<int> ExecuteAsync(SqlBuilder.Template query)
		{
			return await ExecuteAsync((conn, tran) => conn.ExecuteAsync(query.RawSql, param: query.Parameters, transaction: tran));
		}

		public async Task<R> ExecuteAsync<R>(Func<DbConnection, DbTransaction, Task<R>> function)
		{
			using (var conn = Connection)
			{
				conn.Open();
				return await function(conn, null);
			}
		}

		public Task<R> ExecuteAsyncWithTransaction<R>(Func<DbConnection, DbTransaction, Task<R>> function)
		{
			return ExecuteAsync(
				(conn, _) => conn.ExecuteAsyncWithTransaction(function)
			);
		}

		public static string ParameterName(string name, string alias = null)
		{
			if (string.IsNullOrEmpty(alias))
				return name;
			else
				return $"{alias}_{name}";
		}

		public static string ColumnReference(string name, string alias = null)
		{
			if (!string.IsNullOrEmpty(alias))
				alias = $"[{alias}].";

			if (name == "*")
				return alias + name;

			return alias + $"[{name}]";
		}

		public static string TableReference<T>(string alias = null)
			=> TableReference(TypeMeta.Get<T>().TableName, alias: alias);

		public static string TableReference(string name, string alias = null)
		{
			if (string.IsNullOrEmpty(alias))
				return $"{name}";
			else
				return $"{name} AS [{alias}]";
		}
	}
}
