using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Simpleverse.Repository.Operations
{
	public interface IDelete<T>
		where T : class
	{
		Task<bool> DeleteAsync(T model);
		Task<int> DeleteAsync(IEnumerable<T> models);
	}

	public interface IDelete<TModel, TFilter, TOptions> : IDelete<TModel>
		where TModel : class
		where TFilter : IFilter, new()
	{
		Task<int> DeleteAsync(Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null);
	}
}