using System.Collections.Generic;
using System.Threading.Tasks;

namespace Simpleverse.Repository.Operations
{
	public interface IAdd<T>
		where T : class
	{
		Task<int> AddAsync(T model);
		Task<int> AddAsync(IEnumerable<T> models);
	}
}
