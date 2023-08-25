using System.Collections.Generic;
using System.Threading.Tasks;

namespace Simpleverse.Repository.Operations
{
	public interface IUpsert<T> : IAdd<T>, IUpdate<T>
		where T : class
	{
		Task<int> UpsertAsync(T model);
		Task<int> UpsertAsync(IEnumerable<T> models);
	}
}
