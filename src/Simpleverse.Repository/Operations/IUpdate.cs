using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Simpleverse.Repository.Operations
{
	public interface IUpdate<T>
		where T : class
	{
		Task<int> UpdateAsync(T model, Action<IEnumerable<T>, IEnumerable<T>, IEnumerable<PropertyInfo>, IEnumerable<PropertyInfo>> outputMap = null);
		Task<int> UpdateAsync(IEnumerable<T> models, Action<IEnumerable<T>, IEnumerable<T>, IEnumerable<PropertyInfo>, IEnumerable<PropertyInfo>> outputMap = null);
	}

	public interface IUpdate<TUpdate, TFilter, TOptions>
		where TUpdate : class
		where TFilter : class
		where TOptions : QueryOptions, new()
	{
		Task<int> UpdateAsync(Action<TUpdate> updateSetup, Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null);
	}
}
