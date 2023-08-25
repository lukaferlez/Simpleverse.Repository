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
	where TUpdate : IFilter, new()
	where TFilter : IFilter, new()
	where TOptions : Options, new()
	{
		Task<int> UpdateAsync(Action<TUpdate> updateSetup, Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null);
	}
}
