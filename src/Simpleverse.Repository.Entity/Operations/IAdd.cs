using System.Collections.Generic;
using System.Threading.Tasks;

namespace Simpleverse.Repository.Entity.Operations
{
	public interface IAdd<T>
		where T : class
	{
		Task<int> AddAsync(T model);
		Task<int> AddAsync(IEnumerable<T> models, bool mapGeneratedValues = false);
	}
}
