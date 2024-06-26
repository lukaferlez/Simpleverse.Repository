﻿using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Simpleverse.Repository.Db
{
	public static class DbConnectionExtensions
	{
		public static Task<IEnumerable<R>> QueryAsync<R>(this IDbConnection conn, SqlBuilder.Template query, IDbTransaction tran = null)
			=> conn.QueryAsync<R>(query.RawSql, param: query.Parameters, transaction: tran);

		public static Task<IEnumerable<dynamic>> QueryAsync(this IDbConnection conn, SqlBuilder.Template query, IDbTransaction tran = null)
			=> conn.QueryAsync(query.RawSql, param: query.Parameters, transaction: tran);

		public static Task<IEnumerable<(TFirst, TSecond)>> QueryAsync<TFirst, TSecond>(this IDbConnection conn, SqlBuilder.Template query, IDbTransaction tran)
			=> conn.QueryAsync<TFirst, TSecond>(query.RawSql, param: query.Parameters, tran: tran);

		public static Task<IEnumerable<(TFirst, TSecond, TThrid)>> QueryAsync<TFirst, TSecond, TThrid>(this IDbConnection conn, SqlBuilder.Template query, IDbTransaction tran = null)
			=> conn.QueryAsync<TFirst, TSecond, TThrid>(query.RawSql, param: query.Parameters, tran: tran);

		public static Task<IEnumerable<(TFirst, TSecond, TThrid, TFourth)>> QueryAsync<TFirst, TSecond, TThrid, TFourth>(this IDbConnection conn, SqlBuilder.Template query, IDbTransaction tran = null)
			=> conn.QueryAsync<TFirst, TSecond, TThrid, TFourth>(query.RawSql, param: query.Parameters, tran: tran);

		public static Task<IEnumerable<(TFirst, TSecond, TThrid, TFourth, TFifth)>> QueryAsync<TFirst, TSecond, TThrid, TFourth, TFifth>(this IDbConnection conn, SqlBuilder.Template query, IDbTransaction tran = null)
			=> conn.QueryAsync<TFirst, TSecond, TThrid, TFourth, TFifth>(query.RawSql, param: query.Parameters, tran: tran);

		public static Task<IEnumerable<(TFirst, TSecond, TThrid, TFourth, TFifth, TSixth)>> QueryAsync<TFirst, TSecond, TThrid, TFourth, TFifth, TSixth>(this IDbConnection conn, SqlBuilder.Template query, IDbTransaction tran = null)
			=> conn.QueryAsync<TFirst, TSecond, TThrid, TFourth, TFifth, TSixth>(query.RawSql, param: query.Parameters, tran: tran);

		public static Task<IEnumerable<(TFirst, TSecond, TThrid, TFourth, TFifth, TSixth, TSeventh)>> QueryAsync<TFirst, TSecond, TThrid, TFourth, TFifth, TSixth, TSeventh>(this IDbConnection conn, SqlBuilder.Template query, IDbTransaction tran = null)
			=> conn.QueryAsync<TFirst, TSecond, TThrid, TFourth, TFifth, TSixth, TSeventh>(query.RawSql, param: query.Parameters, tran: tran);

		public static Task<IEnumerable<(TFirst, TSecond)>> QueryAsync<TFirst, TSecond>(this IDbConnection conn, string rawSql, object param = null, IDbTransaction tran = null)
			=> conn.QueryAsync<TFirst, TSecond, (TFirst, TSecond)>(rawSql, (first, second) => (first, second), param: param, transaction: tran);

		public static Task<IEnumerable<(TFirst, TSecond, TThird)>> QueryAsync<TFirst, TSecond, TThird>(this IDbConnection conn, string rawSql, object param = null, IDbTransaction tran = null)
			=> conn.QueryAsync<TFirst, TSecond, TThird, (TFirst, TSecond, TThird)>(rawSql, (first, second, third) => (first, second, third), param: param, transaction: tran);

		public static Task<IEnumerable<(TFirst, TSecond, TThird, TFourth)>> QueryAsync<TFirst, TSecond, TThird, TFourth>(this IDbConnection conn, string rawSql, object param = null, IDbTransaction tran = null)
			=> conn.QueryAsync<TFirst, TSecond, TThird, TFourth, (TFirst, TSecond, TThird, TFourth)>(rawSql, (first, second, third, fourth) => (first, second, third, fourth), param: param, transaction: tran);

		public static Task<IEnumerable<(TFirst, TSecond, TThird, TFourth, TFifth)>> QueryAsync<TFirst, TSecond, TThird, TFourth, TFifth>(this IDbConnection conn, string rawSql, object param, IDbTransaction tran = null)
			 => conn.QueryAsync<TFirst, TSecond, TThird, TFourth, TFifth, (TFirst, TSecond, TThird, TFourth, TFifth)>(rawSql, (first, second, third, fourth, fifth) => (first, second, third, fourth, fifth), param: param, transaction: tran);

		public static Task<IEnumerable<(TFirst, TSecond, TThird, TFourth, TFifth, TSixth)>> QueryAsync<TFirst, TSecond, TThird, TFourth, TFifth, TSixth>(this IDbConnection conn, string rawSql, object param = null, IDbTransaction tran = null)
			 => conn.QueryAsync<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, (TFirst, TSecond, TThird, TFourth, TFifth, TSixth)>(rawSql, (first, second, third, fourth, fifth, sixth) => (first, second, third, fourth, fifth, sixth), param: param, transaction: tran);

		public static Task<IEnumerable<(TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh)>> QueryAsync<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh>(this IDbConnection conn, string rawSql, object param = null, IDbTransaction tran = null)
			=> conn.QueryAsync<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, (TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh)>(rawSql, (first, second, third, fourth, fifth, sixth, seventh) => (first, second, third, fourth, fifth, sixth, seventh), param: param, transaction: tran);

		public static Task<int> ExecuteAsync(this IDbConnection conn, SqlBuilder.Template query, IDbTransaction tran = null)
			=> conn.ExecuteAsync(query.RawSql, param: query.Parameters, transaction: tran);

		public static async Task<R> ExecuteAsync<R>(this IDbConnection conn, Func<IDbConnection, Task<R>> function)
		{
			var connectonWasClosed = conn.State == ConnectionState.Closed;
			if (connectonWasClosed)
				conn.Open();

			try
			{
				return await function(conn);
			}
			finally
			{
				if (connectonWasClosed)
					conn.Close();
			}
		}

		public static Task<R> ExecuteAsyncWithTransaction<R>(this IDbConnection connection, Func<IDbConnection, IDbTransaction, Task<R>> function, IDbTransaction transaction = null)
		{
			return connection.ExecuteAsync(
				async (conn) =>
				{
					var transactionWasClosed = transaction == null;
					if (transactionWasClosed)
						transaction = conn.BeginTransaction(IsolationLevel.ReadCommitted);

					try
					{
						var result = await function(conn, transaction);

						// Transaction even after calling BeginTransaction if a mocked DbConnection is used
						if (transactionWasClosed && transaction != null)
							transaction.Commit();

						return result;
					}
					catch
					{
						if (transactionWasClosed && transaction != null)
							transaction.Rollback();
						throw;
					}
					finally
					{
						if (transactionWasClosed && transaction != null)
							transaction.Dispose();
					}
				}
			);
		}
	}
}
