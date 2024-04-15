using Simpleverse.Repository.Entity;

namespace Simpleverse.Repository.Db.Entity
{
	public interface IProjectedEntityDb<TProjection, TModel, TUpdate, TFilter, TOptions>
		: IEntityDb<TProjection, TUpdate, TFilter, TOptions>
		where TProjection : class, IProject<TModel>
		where TModel : class, new()
		where TFilter : class
		where TUpdate : class
		where TOptions : DbQueryOptions, new()
	{
	}

	public interface IProjectedEntityDb<TProjection, TModel, TFilter, TOptions>
		: IProjectedEntityDb<TProjection, TModel, TModel, TFilter, TOptions>
		where TProjection : class, IProject<TModel>
		where TModel : class, new()
		where TFilter : class
		where TOptions : DbQueryOptions, new()
	{
	}

	public interface IProjectedEntityDb<TProjection, TModel, TOptions>
		: IProjectedEntityDb<TProjection, TModel, TModel, TOptions>
		where TProjection : class, IProject<TModel>
		where TModel : class, new()
		where TOptions : DbQueryOptions, new()
	{
	}

	public interface IProjectedEntityDb<TProjection, TModel>
		: IProjectedEntityDb<TProjection, TModel, DbQueryOptions>
		where TProjection : class, IProject<TModel>
		where TModel : class, new()
	{
	}
}
