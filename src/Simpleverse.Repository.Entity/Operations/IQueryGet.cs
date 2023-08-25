using System;
using System.Threading.Tasks;

namespace Simpleverse.Repository.Entity.Operations
{
	public interface IQueryGet<TModel, TFilter, TOptions>
		where TModel : class
		where TFilter : IFilter, new()
		where TOptions : Options, new()
	{
		Task<TModel> GetAsync(Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null);

		Task<T> GetAsync<T>(Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null);
	}
}
