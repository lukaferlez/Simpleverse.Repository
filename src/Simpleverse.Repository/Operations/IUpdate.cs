using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Simpleverse.Repository.Operations
{
	public interface IUpdate<T>
		where T : class
	{
		Task<bool> UpdateAsync(T model);
		Task<int> UpdateAsync(IEnumerable<T> models, bool mapGeneratedValues = false);
	}

	public interface IUpdate<TUpdate, TFilter, TOptions>
	where TUpdate : IQueryFilter, new()
	where TFilter : IQueryFilter, new()
	where TOptions : QueryOptions, new()
	{
		Task<int> UpdateAsync(Action<TUpdate> updateSetup, Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null);
	}
}
