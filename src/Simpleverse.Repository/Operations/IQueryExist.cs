using System;
using System.Threading.Tasks;

namespace Simpleverse.Repository.Operations
{
	public interface IQueryExist<TFilter>
		where TFilter : IFilter, new()
	{
		Task<bool> ExistsAsync(Action<TFilter> filterSetup = null);
	}
}
