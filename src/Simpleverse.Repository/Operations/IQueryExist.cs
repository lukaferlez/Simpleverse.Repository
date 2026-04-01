using System;
using System.Threading;
using System.Threading.Tasks;

namespace Simpleverse.Repository.Operations
{
	public interface IQueryExist<TFilter>
		where TFilter : class
	{
		Task<bool> ExistsAsync(Action<TFilter> filterSetup = null, CancellationToken cancellationToken = default);
	}
}
