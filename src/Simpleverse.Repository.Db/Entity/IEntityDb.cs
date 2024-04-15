using Simpleverse.Repository.Db.Operations;

namespace Simpleverse.Repository.Db.Entity
{
	public interface IEntityDb<TModel, TUpdate, TFilter, TOptions>
		:
			IEntity<TModel, TUpdate, TFilter, TOptions>,
			IAddDb<TModel>,
			IAggregateDb<TFilter>,
			IDeleteDb<TModel, TFilter, TOptions>,
			IReplaceDb<TModel, TFilter>,
			IUpdateDb<TUpdate, TFilter, TOptions>,
			IUpsertDb<TModel>
		where TModel : class
		where TUpdate : class
		where TFilter : class
		where TOptions : DbQueryOptions, new()
	{
	}
}
