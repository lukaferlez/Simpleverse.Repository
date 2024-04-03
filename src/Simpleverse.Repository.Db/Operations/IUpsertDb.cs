using Simpleverse.Repository.Operations;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Threading.Tasks;

namespace Simpleverse.Repository.Db.Operations
{
	public interface IUpsertDb<T> : IUpsert<T>
		where T : class
	{
		Task<int> UpsertAsync(IDbConnection connection, IEnumerable<T> models, Action<IEnumerable<T>, IEnumerable<T>, IEnumerable<PropertyInfo>, IEnumerable<PropertyInfo>> outputMap = null, IDbTransaction transaction = null);
	}
}
