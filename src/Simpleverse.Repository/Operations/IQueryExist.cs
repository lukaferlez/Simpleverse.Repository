using System;
using System.Threading.Tasks;

namespace Simpleverse.Repository.Operations
{
	public interface IQueryExist<TFilter>
		where TFilter : IQueryFilter, new()
	{
		Task<bool> ExistsAsync(Action<TFilter> filterSetup = null);
	}
}
