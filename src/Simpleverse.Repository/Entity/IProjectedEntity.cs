namespace Simpleverse.Repository.Entity
{
	public interface IProjectedEntity<TProjection, TModel, TUpdate, TFilter, TOptions>
		: IEntity<TProjection, TUpdate, TFilter, TOptions>
		where TProjection : class, IProject<TModel>
		where TModel : class, new()
		where TFilter : class
		where TUpdate : class
		where TOptions : class, new()
	{
	}

	public interface IProjectedEntity<TProjection, TModel, TFilter, TOptions>
		: IProjectedEntity<TProjection, TModel, TModel, TFilter, TOptions>
		where TProjection : class, IProject<TModel>
		where TModel : class, new()
		where TFilter : class
		where TOptions : class, new()
	{
	}

	public interface IProjectedEntity<TProjection, TModel, TOptions>
		: IProjectedEntity<TProjection, TModel, TModel, TOptions>
		where TProjection : class, IProject<TModel>
		where TModel : class, new()
		where TOptions : class, new()
	{
	}

	public interface IProjectedEntity<TProjection, TModel>
		: IProjectedEntity<TProjection, TModel, QueryOptions>
		where TProjection : class, IProject<TModel>
		where TModel : class, new()
	{
	}
}
