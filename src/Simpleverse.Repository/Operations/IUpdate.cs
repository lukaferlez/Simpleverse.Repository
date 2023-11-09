using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Simpleverse.Repository.Operations
{
	public interface IUpdate<T>
		where T : class
	{
		Task<bool> UpdateAsync(T model);
		Task<int> UpdateAsync(IEnumerable<T> models, Action<IEnumerable<T>, IEnumerable<T>, IEnumerable<PropertyInfo>, IEnumerable<PropertyInfo>> outputMap = null);
	}

	public interface IUpdate<TUpdate, TFilter, TOptions>
	where TUpdate : IQueryFilter, new()
	where TFilter : IQueryFilter, new()
	where TOptions : QueryOptions, new()
	{
		Task<int> UpdateAsync(Action<TUpdate> updateSetup, Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null);
	}
}
