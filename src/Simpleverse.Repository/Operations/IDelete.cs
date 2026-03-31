using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Simpleverse.Repository.Operations
{
	public interface IDelete<T>
		where T : class
	{
		Task<bool> DeleteAsync(T model, CancellationToken cancellationToken = default);
		Task<int> DeleteAsync(IEnumerable<T> models, CancellationToken cancellationToken = default);
	}

	public interface IDelete<TModel, TFilter, TOptions> : IDelete<TModel>
		where TModel : class
		where TFilter : class
		where TOptions : class
	{
		Task<int> DeleteAsync(Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null, CancellationToken cancellationToken = default);
	}
}