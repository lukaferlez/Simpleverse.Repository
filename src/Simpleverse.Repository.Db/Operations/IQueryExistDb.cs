using Simpleverse.Repository.Operations;
using System;
using System.Data;
using System.Threading.Tasks;

namespace Simpleverse.Repository.Db.Operations
{
	public interface IQueryExistDb<TFilter> : IQueryExist<TFilter>
		where TFilter : class
	{
		Task<bool> ExistsAsync(IDbConnection connection, Action<TFilter> filterSetup = null, IDbTransaction transaction = null);
	}
}
