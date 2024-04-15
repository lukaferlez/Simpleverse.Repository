using Simpleverse.Repository.Operations;

namespace Simpleverse.Repository.Entity
{
	public interface IEntity<TModel, TUpdate, TFilter, TOptions>
		: IAdd<TModel>,
		IAggregate<TFilter>,
		IDelete<TModel, TFilter, TOptions>,
		IQuery<TModel, TFilter, TOptions>,
		IReplace<TModel, TFilter>,
		IUpdate<TUpdate, TFilter, TOptions>,
		IUpsert<TModel>
		where TModel : class
		where TUpdate : class
		where TFilter : class
		where TOptions : class, new()
	{
	}

	public interface IEntity<TModel, TFilter, TOptions>
		: IEntity<TModel, TModel, TFilter, TOptions>
		where TModel : class
		where TFilter : class
		where TOptions : class, new()
	{

	}

	public interface IEntity<TModel, TOptions>
		: IEntity<TModel, TModel, TOptions>
		where TModel : class
		where TOptions : class, new()
	{

	}

	public interface IEntity<TModel>
		: IEntity<TModel, QueryOptions>
		where TModel : class
	{

	}
}
