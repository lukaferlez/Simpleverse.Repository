using System;
using System.Threading;
using System.Threading.Tasks;

namespace Simpleverse.Repository.Operations
{
	public interface IAggregate
	{
		Task<TResult?> MaxAsync<TResult>(string columnName, CancellationToken cancellationToken = default)
			where TResult : struct;

		Task<TResult?> MinAsync<TResult>(string columnName, CancellationToken cancellationToken = default)
			where TResult : struct;
	}

	public interface IAggregate<TFilter> : IAggregate
	{
		Task<TResult?> MaxAsync<TResult>(string columnName, Action<TFilter> filterSetup = null, CancellationToken cancellationToken = default)
			where TResult : struct;

		Task<TResult?> MinAsync<TResult>(string columnName, Action<TFilter> filterSetup = null, CancellationToken cancellationToken = default)
			where TResult : struct;
	}
}
