using Simpleverse.Repository.Db.Operations;
using Simpleverse.Repository.Entity;

namespace Simpleverse.Repository.Db.Entity
{
	public interface IEntityDb<TModel, TUpdate, TFilter, TOptions>
		: IEntity<TModel, TUpdate, TFilter, TOptions>,
			IAddDb<TModel>,
			IAggregateDb<TFilter>,
			IDeleteDb<TModel>,
			IDeleteDb<TModel, TFilter, TOptions>,
			IQueryDb<TModel, TFilter, TOptions>,
			IReplaceDb<TModel, TFilter>,
			IUpdateDb<TModel>,
			IUpdateDb<TUpdate, TFilter, TOptions>,
			IUpsertDb<TModel>
		where TModel : class
		where TUpdate : class
		where TFilter : class
		where TOptions : class, new()
	{
	}

	public interface IEntityDb<TModel, TFilter, TOptions>
		: IEntityDb<TModel, TModel, TFilter, TOptions>, IEntity<TModel, TFilter, TOptions>
		where TModel : class
		where TFilter : class
		where TOptions : class, new()
	{
	}

	public interface IEntityDb<TModel, TOptions>
		: IEntityDb<TModel, TModel, TOptions>, IEntity<TModel, TOptions>
		where TModel : class
		where TOptions : class, new()
	{
	}

	public interface IEntityDb<TModel>
		: IEntityDb<TModel, DbQueryOptions>
		where TModel : class
	{

	}
}
