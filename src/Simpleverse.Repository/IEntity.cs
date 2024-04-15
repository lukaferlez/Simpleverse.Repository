using Simpleverse.Repository.Operations;

namespace Simpleverse.Repository
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
		where TOptions : class
	{
	}
}
