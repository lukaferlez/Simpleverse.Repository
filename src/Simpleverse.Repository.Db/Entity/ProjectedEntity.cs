using Simpleverse.Repository.Entity;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Simpleverse.Repository.Db.Entity
{
	public class ProjectedEntity<TProjection, TModel, TUpdate, TFilter, TOptions>
		: ProjectedEntity<TProjection, IEntity<TModel, TUpdate, TFilter, TOptions>, TModel, TUpdate, TFilter, TOptions>,
		IProjectedEntity<TProjection, TModel, TUpdate, TFilter, TOptions>
		where TProjection : class, IProject<TModel>
		where TModel : class, new()
		where TFilter : class
		where TUpdate : class
		where TOptions : DbQueryOptions, new()
	{
		public ProjectedEntity(IEntity<TModel, TUpdate, TFilter, TOptions> entity)
			: base(entity)
		{
		}

		public ProjectedEntity(IEntity<TModel, TUpdate, TFilter, TOptions> entity, Func<TModel, TProjection> creator)
			: base(entity, creator)
		{
		}

		public Task<int> AddAsync(
			IDbConnection connection,
			IEnumerable<TProjection> models,
			Action<IEnumerable<TProjection>, IEnumerable<TProjection>, IEnumerable<PropertyInfo>, IEnumerable<PropertyInfo>> outputMap = null,
			IDbTransaction transaction = null
		)
		{
			return _entity.AddAsync(connection, models.Select(x => x.Model), OutputMapRedirect(models, outputMap), transaction: transaction);
		}

		public Task<int> DeleteAsync(IDbConnection connection, Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null, IDbTransaction transaction = null)
		{
			return _entity.DeleteAsync(connection, filterSetup, optionsSetup, transaction: transaction);
		}

		public Task<bool> DeleteAsync(IDbConnection connection, TProjection model, IDbTransaction transaction = null)
		{
			return _entity.DeleteAsync(connection, model.Model, transaction: transaction);
		}

		public Task<int> DeleteAsync(IDbConnection connection, IEnumerable<TProjection> models, IDbTransaction transaction = null)
		{
			return _entity.DeleteAsync(connection, models.Select(x => x.Model), transaction: transaction);
		}

		public Task<bool> ExistsAsync(IDbConnection connection, Action<TFilter> filterSetup = null, IDbTransaction transaction = null)
		{
			return _entity.ExistsAsync(connection, filterSetup, transaction: transaction);
		}

		public async Task<TProjection> GetAsync(IDbConnection connection, Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null, IDbTransaction transaction = null)
		{
			var model = await _entity.GetAsync(connection, filterSetup, optionsSetup, transaction: transaction);
			if (model == null)
				return default;

			return Instance(model);
		}

		public Task<T> GetAsync<T>(IDbConnection connection, Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null, IDbTransaction transaction = null)
		{
			return _entity.GetAsync<T>(connection, filterSetup, optionsSetup, transaction: transaction);
		}

		public async Task<IEnumerable<TProjection>> ListAsync(IDbConnection connection, Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null, IDbTransaction transaction = null)
		{
			var models = await _entity.ListAsync(connection, filterSetup, optionsSetup, transaction);
			if (models == null)
				return default;

			return models.Select(Instance);
		}

		public Task<IEnumerable<T>> ListAsync<T>(IDbConnection connection, Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null, IDbTransaction transaction = null)
		{
			return _entity.ListAsync<T>(connection, filterSetup, optionsSetup, transaction: transaction);
		}

		public async Task<IEnumerable<TProjection>> ListAsync(IDbConnection connection, TFilter filter, TOptions options, IDbTransaction transaction = null)
		{
			var models = await _entity.ListAsync(connection, filter, options, transaction);
			if (models == null)
				return default;

			return models.Select(Instance);
		}

		public Task<IEnumerable<T>> ListAsync<T>(IDbConnection connection, TFilter filter, TOptions options, IDbTransaction transaction = null)
		{
			return _entity.ListAsync<T>(connection, filter, options, transaction: transaction);
		}

		public Task<TResult?> MaxAsync<TResult>(IDbConnection connection, string columName, Action<TFilter> filterSetup, IDbTransaction transaction = null) where TResult : struct
			=> _entity.MaxAsync<TResult>(connection, columName, filterSetup, transaction: transaction);

		public Task<TResult?> MaxAsync<TResult>(IDbConnection connection, string columnName, IDbTransaction transaction = null) where TResult : struct
			=> _entity.MaxAsync<TResult>(connection, columnName, transaction: transaction);

		public Task<TResult?> MinAsync<TResult>(IDbConnection connection, string columName, Action<TFilter> filterSetup, IDbTransaction transaction = null) where TResult : struct
			=> _entity.MinAsync<TResult>(connection, columName, filterSetup, transaction: transaction);

		public Task<TResult?> MinAsync<TResult>(IDbConnection connection, string columnName, IDbTransaction transaction = null) where TResult : struct
			=> _entity.MinAsync<TResult>(connection, columnName, transaction: transaction);

		public Task<(int Deleted, int Added)> ReplaceAsync(IDbConnection conn, IDbTransaction tran, Action<TFilter> filterSetup, IEnumerable<TProjection> models)
			=> _entity.ReplaceAsync(conn, tran, filterSetup, models.Select(x => x.Model));

		public Task<int> UpdateAsync(IDbConnection connection, Action<TUpdate> updateSetup, Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null, IDbTransaction transaction = null)
			=> _entity.UpdateAsync(connection, updateSetup, filterSetup, optionsSetup, transaction: transaction);

		public Task<int> UpdateAsync(IDbConnection connection, IEnumerable<TProjection> models, Action<IEnumerable<TProjection>, IEnumerable<TProjection>, IEnumerable<PropertyInfo>, IEnumerable<PropertyInfo>> outputMap = null, IDbTransaction transaction = null)
			=> _entity.UpdateAsync(connection, models.Select(x => x.Model), OutputMapRedirect(models, outputMap), transaction);

		public Task<int> UpsertAsync(IDbConnection connection, IEnumerable<TProjection> models, Action<IEnumerable<TProjection>, IEnumerable<TProjection>, IEnumerable<PropertyInfo>, IEnumerable<PropertyInfo>> outputMap = null, IDbTransaction transaction = null)
			=> _entity.UpsertAsync(connection, models.Select(x => x.Model), outputMap: OutputMapRedirect(models, outputMap), transaction: transaction);
	}

	public class ProjectedEntity<TProjection, TModel, TFilter, TOptions>
		: ProjectedEntity<TProjection, TModel, TModel, TFilter, TOptions>, IProjectedEntity<TProjection, TModel, TFilter, TOptions>
		where TProjection : class, IProject<TModel>
		where TModel : class, new()
		where TFilter : class
		where TOptions : DbQueryOptions, new()
	{
		public ProjectedEntity(IEntity<TModel, TFilter, TOptions> entity)
			: base(entity)
		{

		}

		public ProjectedEntity(IEntity<TModel, TFilter, TOptions> entity, Func<TModel, TProjection> creator)
			: base(entity, creator)
		{
		}
	}

	public class ProjectedEntity<TProjection, TModel, TOptions>
		: ProjectedEntity<TProjection, TModel, TModel, TOptions>, IProjectedEntity<TProjection, TModel, TOptions>
		where TProjection : class, IProject<TModel>
		where TModel : class, new()
		where TOptions : DbQueryOptions, new()
	{
		public ProjectedEntity(IEntity<TModel, TModel, TOptions> entity)
			: base(entity)
		{
		}

		public ProjectedEntity(IEntity<TModel, TModel, TOptions> entity, Func<TModel, TProjection> creator)
			: base(entity, creator)
		{
		}
	}

	public class ProjectedEntity<TProjection, TModel>
		: ProjectedEntity<TProjection, TModel, DbQueryOptions>, IProjectedEntity<TProjection, TModel>
		where TProjection : class, IProject<TModel>
		where TModel : class, new()
	{
		public ProjectedEntity(IEntity<TModel> entity)
			: base(entity)
		{
		}

		public ProjectedEntity(IEntity<TModel> entity, Func<TModel, TProjection> creator)
			: base(entity, creator)
		{
		}
	}
}
