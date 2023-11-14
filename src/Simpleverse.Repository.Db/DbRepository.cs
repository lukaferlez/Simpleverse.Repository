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

		public Task<IEnumerable<(TFirst, TSecond, TThrid, TFourth)>> QueryAsync<TFirst, TSecond, TThrid, TFourth>(SqlBuilder.Template query)
			=> QueryAsync<TFirst, TSecond, TThrid, TFourth>(query.RawSql, query.Parameters);

		public Task<IEnumerable<(TFirst, TSecond, TThrid, TFourth, TFifth)>> QueryAsync<TFirst, TSecond, TThrid, TFourth, TFifth>(SqlBuilder.Template query)
			=> QueryAsync<TFirst, TSecond, TThrid, TFourth, TFifth>(query.RawSql, query.Parameters);

		public Task<IEnumerable<(TFirst, TSecond, TThrid, TFourth, TFifth, TSixth)>> QueryAsync<TFirst, TSecond, TThrid, TFourth, TFifth, TSixth>(SqlBuilder.Template query)
			=> QueryAsync<TFirst, TSecond, TThrid, TFourth, TFifth, TSixth>(query.RawSql, query.Parameters);

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

		public async Task<IEnumerable<(TFirst, TSecond, TThird, TFourth)>> QueryAsync<TFirst, TSecond, TThird, TFourth>(string rawSql, object parameters)
		{
			return await ExecuteAsync((conn, tran) => conn.QueryAsync<TFirst, TSecond, TThird, TFourth, (TFirst, TSecond, TThird, TFourth)>(rawSql, (first, second, third, fourth) => (first, second, third, fourth), param: parameters, transaction: tran));
		}

		public async Task<IEnumerable<(TFirst, TSecond, TThird, TFourth, TFifth)>> QueryAsync<TFirst, TSecond, TThird, TFourth, TFifth>(string rawSql, object parameters)
		{
			return await ExecuteAsync((conn, tran) => conn.QueryAsync<TFirst, TSecond, TThird, TFourth, TFifth, (TFirst, TSecond, TThird, TFourth, TFifth)>(rawSql, (first, second, third, fourth, fifth) => (first, second, third, fourth, fifth), param: parameters, transaction: tran));
		}

		public async Task<IEnumerable<(TFirst, TSecond, TThird, TFourth, TFifth, TSixth)>> QueryAsync<TFirst, TSecond, TThird, TFourth, TFifth, TSixth>(string rawSql, object parameters)
		{
			return await ExecuteAsync((conn, tran) => conn.QueryAsync<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, (TFirst, TSecond, TThird, TFourth, TFifth, TSixth)>(rawSql, (first, second, third, fourth, fifth, sixth) => (first, second, third, fourth, fifth, sixth), param: parameters, transaction: tran));
		}

		public async Task<IEnumerable<(TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh)>> QueryAsync<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh>(string rawSql, object parameters)
		{
			return await ExecuteAsync((conn, tran) => conn.QueryAsync<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, (TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh)>(rawSql, (first, second, third, fourth, fifth, sixth, seventh) => (first, second, third, fourth, fifth, sixth, seventh), param: parameters, transaction: tran));
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
