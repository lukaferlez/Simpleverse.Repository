using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Simpleverse.Repository.Operations
{
	public interface IQueryList<TModel, TFilter, TOptions>
		where TModel : class
		where TFilter : class
		where TOptions : class
	{
		Task<IEnumerable<TModel>> ListAsync(Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null, CancellationToken cancellationToken = default);

		Task<IEnumerable<T>> ListAsync<T>(Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null, CancellationToken cancellationToken = default);

		Task<IEnumerable<TModel>> ListAsync(TFilter filter, TOptions options, CancellationToken cancellationToken = default);

		Task<IEnumerable<T>> ListAsync<T>(TFilter filter, TOptions options, CancellationToken cancellationToken = default);
	}
}
