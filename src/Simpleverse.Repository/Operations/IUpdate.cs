using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Simpleverse.Repository.Operations
{
	public interface IUpdate<T>
		where T : class
	{
		Task<int> UpdateAsync(T model, CancellationToken cancellationToken = default);
		Task<int> UpdateAsync(IEnumerable<T> models, CancellationToken cancellationToken = default);
	}

	public interface IUpdate<TUpdate, TFilter, TOptions>
		where TUpdate : class
		where TFilter : class
		where TOptions : class
	{
		Task<int> UpdateAsync(Action<TUpdate> updateSetup, Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null, CancellationToken cancellationToken = default);
	}
}
