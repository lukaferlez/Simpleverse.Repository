using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Simpleverse.Repository.Operations
{
	public interface IReplace<TModel, TFilter>
		where TModel : class
		where TFilter : IQueryFilter, new()
	{
		Task<(int Deleted, int Added)> ReplaceAsync(
			Action<TFilter> filterSetup,
			IEnumerable<TModel> models
		);
	}
}
