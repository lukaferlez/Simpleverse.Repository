using Simpleverse.Repository.Operations;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Threading.Tasks;

namespace Simpleverse.Repository.Db.Operations
{
	public interface IAddDb<T> : IAdd<T>
		where T : class
	{
		Task<int> AddAsync(IDbConnection connection, T model, IDbTransaction transaction = null);
		Task<int> AddAsync(
			IDbConnection connection,
			IEnumerable<T> models,
			Action<IEnumerable<T>, IEnumerable<T>, IEnumerable<PropertyInfo>, IEnumerable<PropertyInfo>> outputMap = null,
			IDbTransaction transaction = null
		);
	}
}
