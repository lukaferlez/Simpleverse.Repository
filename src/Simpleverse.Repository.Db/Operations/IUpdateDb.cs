using Simpleverse.Repository.Operations;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Threading.Tasks;

namespace Simpleverse.Repository.Db.Operations
{
	public interface IUpdateDb<T> : IUpdate<T>
		where T : class
	{
		Task<int> UpdateAsync(IEnumerable<T> models, Action<IEnumerable<T>, IEnumerable<T>, IEnumerable<PropertyInfo>, IEnumerable<PropertyInfo>> outputMap);
		Task<int> UpdateAsync(
			IDbConnection connection,
			IEnumerable<T> models,
			Action<IEnumerable<T>, IEnumerable<T>, IEnumerable<PropertyInfo>, IEnumerable<PropertyInfo>> outputMap = null,
			IDbTransaction transaction = null
		);
	}

	public interface IUpdateDb<TUpdate, TFilter, TOptions> : IUpdate<TUpdate, TFilter, TOptions>
		where TUpdate : class
		where TFilter : class
		where TOptions : class, new()
	{
		Task<int> UpdateAsync(IDbConnection connection, Action<TUpdate> updateSetup, Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null, IDbTransaction transaction = null);
	}
}
