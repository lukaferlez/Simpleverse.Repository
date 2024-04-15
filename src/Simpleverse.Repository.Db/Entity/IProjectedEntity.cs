using Simpleverse.Repository.Entity;

namespace Simpleverse.Repository.Db.Entity
{
	public interface IProjectedEntity<TProjection, TModel, TUpdate, TFilter, TOptions>
		: IEntity<TProjection, TUpdate, TFilter, TOptions>, Repository.Entity.IProjectedEntity<TProjection, TModel, TUpdate, TFilter, TOptions>
		where TProjection : class, IProject<TModel>
		where TModel : class, new()
		where TFilter : class
		where TUpdate : class
		where TOptions : DbQueryOptions, new()
	{
	}

	public interface IProjectedEntity<TProjection, TModel, TFilter, TOptions>
		: IProjectedEntity<TProjection, TModel, TModel, TFilter, TOptions>, Repository.Entity.IProjectedEntity<TProjection, TModel, TFilter, TOptions>
		where TProjection : class, IProject<TModel>
		where TModel : class, new()
		where TFilter : class
		where TOptions : DbQueryOptions, new()
	{
	}

	public interface IProjectedEntity<TProjection, TModel, TOptions>
		: IProjectedEntity<TProjection, TModel, TModel, TOptions>, Repository.Entity.IProjectedEntity<TProjection, TModel, TOptions>
		where TProjection : class, IProject<TModel>
		where TModel : class, new()
		where TOptions : DbQueryOptions, new()
	{
	}

	public interface IProjectedEntity<TProjection, TModel>
		: IProjectedEntity<TProjection, TModel, DbQueryOptions>
		where TProjection : class, IProject<TModel>
		where TModel : class, new()
	{
	}
}
