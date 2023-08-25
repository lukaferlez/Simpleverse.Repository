namespace Simpleverse.Repository.Entity.Operations
{
	public interface IQuery<TModel, TFilter, TOptions>
		: IQueryExist<TFilter>, IQueryGet<TModel, TFilter, TOptions>, IQueryList<TModel, TFilter, TOptions>
		where TModel : class
		where TFilter : IFilter, new()
		where TOptions : Options, new()
	{
	}
}
