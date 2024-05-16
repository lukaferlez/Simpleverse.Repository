using Dapper;
using Simpleverse.Repository.Db.Meta;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace Simpleverse.Repository.Db
{
	public class DbRepository : IDbRepository
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

		public Task<IEnumerable<(TFirst, TSecond, TThrid, TFourth, TFifth, TSixth, TSeventh)>> QueryAsync<TFirst, TSecond, TThrid, TFourth, TFifth, TSixth, TSeventh>(SqlBuilder.Template query)
			=> QueryAsync<TFirst, TSecond, TThrid, TFourth, TFifth, TSixth, TSeventh>(query.RawSql, query.Parameters);

		public virtual async Task<IEnumerable<R>> QueryAsync<R>(string rawSql, object parameters)
		{
			return await ExecuteAsync((conn) => conn.QueryAsync<R>(rawSql, param: parameters));
		}

		public virtual async Task<IEnumerable<dynamic>> QueryAsync(string rawSql, object parameters)
		{
			return await ExecuteAsync((conn) => conn.QueryAsync(rawSql, param: parameters));
		}

		public Task<IEnumerable<(TFirst, TSecond)>> QueryAsync<TFirst, TSecond>(string rawSql, object parameters = null)
			=> ExecuteAsync((conn) => conn.QueryAsync<TFirst, TSecond>(rawSql, param: parameters));

		public Task<IEnumerable<(TFirst, TSecond, TThird)>> QueryAsync<TFirst, TSecond, TThird>(string rawSql, object parameters = null)
			=> ExecuteAsync((conn) => conn.QueryAsync<TFirst, TSecond, TThird>(rawSql, param: parameters));

		public Task<IEnumerable<(TFirst, TSecond, TThird, TFourth)>> QueryAsync<TFirst, TSecond, TThird, TFourth>(string rawSql, object parameters = null)
			=> ExecuteAsync((conn) => conn.QueryAsync<TFirst, TSecond, TThird, TFourth>(rawSql, param: parameters));

		public Task<IEnumerable<(TFirst, TSecond, TThird, TFourth, TFifth)>> QueryAsync<TFirst, TSecond, TThird, TFourth, TFifth>(string rawSql, object parameters = null)
			=> ExecuteAsync((conn) => conn.QueryAsync<TFirst, TSecond, TThird, TFourth, TFifth>(rawSql, param: parameters));

		public Task<IEnumerable<(TFirst, TSecond, TThird, TFourth, TFifth, TSixth)>> QueryAsync<TFirst, TSecond, TThird, TFourth, TFifth, TSixth>(string rawSql, object parameters = null)
			=> ExecuteAsync((conn) => conn.QueryAsync<TFirst, TSecond, TThird, TFourth, TFifth, TSixth>(rawSql, param: parameters));

		public Task<IEnumerable<(TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh)>> QueryAsync<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh>(string rawSql, object parameters = null)
			=> ExecuteAsync((conn) => conn.QueryAsync<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh>(rawSql, param: parameters));


		public Task<int> ExecuteAsync(SqlBuilder.Template query)
			=> ExecuteAsync((conn) => conn.ExecuteAsync(query));

		public async Task<R> ExecuteAsync<R>(Func<IDbConnection, Task<R>> function)
		{
			using (var conn = Connection)
			{
				conn.Open();
				return await function(conn);
			}
		}

		public Task<R> ExecuteAsyncWithTransaction<R>(Func<IDbConnection, IDbTransaction, Task<R>> function)
		{
			return ExecuteAsync(
				(conn) => conn.ExecuteAsyncWithTransaction(function)
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
