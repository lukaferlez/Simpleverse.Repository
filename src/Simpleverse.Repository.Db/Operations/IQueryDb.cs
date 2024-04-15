using Simpleverse.Repository.Operations;

namespace Simpleverse.Repository.Db.Operations
{
	public interface IQueryDb<TModel, TFilter, TOptions>
		: IQuery<TModel, TFilter, TOptions>,
			IQueryExistDb<TFilter>,
			IQueryGetDb<TModel, TFilter, TOptions>,
			IQueryListDb<TModel, TFilter, TOptions>
		where TModel : class
		where TFilter : class
		where TOptions : class, new()
	{
	}
}
