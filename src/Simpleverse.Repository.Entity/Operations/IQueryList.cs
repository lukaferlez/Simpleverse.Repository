using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Simpleverse.Repository.Entity.Operations
{
	public interface IQueryList<TModel, TFilter, TOptions>
		where TModel : class
		where TFilter : IFilter, new()
		where TOptions : Options, new()
	{
		Task<IEnumerable<TModel>> ListAsync(Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null);

		Task<IEnumerable<T>> ListAsync<T>(Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null);

		Task<IEnumerable<TModel>> ListAsync(TFilter filter, TOptions options);

		Task<IEnumerable<T>> ListAsync<T>(TFilter filter, TOptions options);
	}
}
