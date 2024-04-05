namespace Simpleverse.Repository.Operations
{
	public interface IQuery<TModel, TFilter, TOptions>
		: IQueryExist<TFilter>, IQueryGet<TModel, TFilter, TOptions>, IQueryList<TModel, TFilter, TOptions>
		where TModel : class
		where TFilter : class
		where TOptions : QueryOptions, new()
	{
	}
}
