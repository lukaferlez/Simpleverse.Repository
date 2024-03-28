using Simpleverse.Repository.Operations;
using System;
using System.Data;
using System.Threading.Tasks;

namespace Simpleverse.Repository.Db.Operations
{
	public interface IAggregateDb : IAggregate
	{
		Task<TResult?> MaxAsync<TResult>(IDbConnection connection, string columnName, IDbTransaction transaction = null)
			where TResult : struct;

		Task<TResult?> MinAsync<TResult>(IDbConnection connection, string columnName, IDbTransaction transaction = null)
			where TResult : struct;
	}

	public interface IAggregateDb<TFilter> : IAggregateDb, IAggregate<TFilter>
	{
		Task<TResult?> MaxAsync<TResult>(IDbConnection connection, string columName, Action<TFilter> filterSetup, IDbTransaction transaction = null)
			where TResult : struct;

		Task<TResult?> MinAsync<TResult>(IDbConnection connection, string columName, Action<TFilter> filterSetup, IDbTransaction transaction = null)
			where TResult : struct;
	}
}
