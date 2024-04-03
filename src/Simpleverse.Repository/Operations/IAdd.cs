using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Simpleverse.Repository.Operations
{
	public interface IAdd<T>
		where T : class
	{
		Task<int> AddAsync(T model, Action<IEnumerable<T>, IEnumerable<T>, IEnumerable<PropertyInfo>, IEnumerable<PropertyInfo>> outputMap = null);
		Task<int> AddAsync(IEnumerable<T> models, Action<IEnumerable<T>, IEnumerable<T>, IEnumerable<PropertyInfo>, IEnumerable<PropertyInfo>> outputMap = null);
	}
}
