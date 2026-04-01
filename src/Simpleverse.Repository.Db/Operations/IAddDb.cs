using Simpleverse.Repository.Operations;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Simpleverse.Repository.Db.Operations
{
	public interface IAddDb<T> : IAdd<T>
		where T : class
	{
		Task<int> AddAsync(IEnumerable<T> models, Action<IEnumerable<T>, IEnumerable<T>, IEnumerable<PropertyInfo>, IEnumerable<PropertyInfo>> outputMap, CancellationToken cancellationToken = default);
		Task<int> AddAsync(
			IDbConnection connection,
			IEnumerable<T> models,
			Action<IEnumerable<T>, IEnumerable<T>, IEnumerable<PropertyInfo>, IEnumerable<PropertyInfo>> outputMap = null,
			IDbTransaction transaction = null,
			CancellationToken cancellationToken = default
		);
	}
}
