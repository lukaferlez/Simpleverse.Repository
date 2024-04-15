namespace Simpleverse.Repository.Db.Entity
{
	public interface IProjectedEntity<TProjection, TModel, TUpdate, TFilter, TOptions>
		: IEntityDb<TProjection, TUpdate, TFilter, TOptions>
		where TProjection : class, IProject<TModel>
		where TModel : class, new()
		where TFilter : class
		where TUpdate : class
		where TOptions : DbQueryOptions, new()
	{
	}

	public interface IProjectedEntity<TProjection, TModel, TFilter, TOptions>
		: IProjectedEntity<TProjection, TModel, TModel, TFilter, TOptions>
		where TProjection : class, IProject<TModel>
		where TModel : class, new()
		where TFilter : class
		where TOptions : DbQueryOptions, new()
	{
	}

	public interface IProjectedEntity<TProjection, TModel, TOptions>
		: IProjectedEntity<TProjection, TModel, TModel, TOptions>
		where TProjection : class, IProject<TModel>
		where TModel : class, new()
		where TOptions : DbQueryOptions, new()
	{
	}

	public interface IProjectedEntity<TProjection, TModel>
		: IProjectedEntity<TProjection, TModel, TModel, DbQueryOptions>
		where TProjection : class, IProject<TModel>
		where TModel : class, new()
	{
	}
}
