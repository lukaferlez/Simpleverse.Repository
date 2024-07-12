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

		#region IAdd

		public Task<int> AddAsync(IEnumerable<TProjection> models, Action<IEnumerable<TProjection>, IEnumerable<TProjection>, IEnumerable<PropertyInfo>, IEnumerable<PropertyInfo>> outputMap)
			=> _entity.ExecuteAsyncWithTransaction((conn, tran) => AddAsync(conn, models, outputMap, tran));

		public virtual Task<int> AddAsync(
			IDbConnection connection,
			IEnumerable<TProjection> models,
			Action<IEnumerable<TProjection>, IEnumerable<TProjection>, IEnumerable<PropertyInfo>, IEnumerable<PropertyInfo>> outputMap = null,
			IDbTransaction transaction = null
		)
		{
			return _entity.AddAsync(connection, models.Select(x => x.Model), OutputMapRedirect(models, outputMap), transaction: transaction);
		}

		#endregion

		#region IDelete

		public override sealed Task<int> DeleteAsync(Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null)
			=> _entity.ExecuteAsyncWithTransaction((conn, tran) => DeleteAsync(conn, filterSetup, optionsSetup, tran));

		public virtual Task<int> DeleteAsync(IDbConnection connection, Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null, IDbTransaction transaction = null)
			=> _entity.DeleteAsync(connection, filterSetup, optionsSetup, transaction: transaction);

		public async Task<bool> DeleteAsync(IDbConnection connection, TProjection model, IDbTransaction transaction = null)
			=> await DeleteAsync(connection, new[] { model }, transaction) > 0;

		public override sealed Task<int> DeleteAsync(IEnumerable<TProjection> models)
			=> _entity.ExecuteAsyncWithTransaction((conn, tran) => DeleteAsync(conn, models, tran));

		public virtual Task<int> DeleteAsync(IDbConnection connection, IEnumerable<TProjection> models, IDbTransaction transaction = null)
		{
			return _entity.DeleteAsync(connection, models.Select(x => x.Model), transaction: transaction);
		}

		#endregion

		#region IQuery

		#region Exists

		public override sealed Task<bool> ExistsAsync(Action<TFilter> filterSetup = null)
			=> _entity.ExecuteAsyncWithTransaction((conn, tran) => ExistsAsync(conn, filterSetup, tran));

		public virtual Task<bool> ExistsAsync(IDbConnection connection, Action<TFilter> filterSetup = null, IDbTransaction transaction = null)
			=> _entity.ExistsAsync(connection, filterSetup, transaction: transaction);

		#endregion

		#region Get

		public async Task<TProjection> GetAsync(IDbConnection connection, Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null, IDbTransaction transaction = null)
		{
			var model = await GetAsync<TModel>(connection, filterSetup, optionsSetup, transaction: transaction);
			if (model == null)
				return default;

			return Instance(model);
		}

		public override sealed async Task<T> GetAsync<T>(Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null)
			=> (await ListAsync<T>(filterSetup, options => { options.Take = 1; optionsSetup?.Invoke(options); })).FirstOrDefault();

		public async Task<T> GetAsync<T>(IDbConnection connection, Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null, IDbTransaction transaction = null)
			=> (await ListAsync<T>(connection, filterSetup, options => { options.Take = 1; optionsSetup?.Invoke(options); }, transaction: transaction)).FirstOrDefault();

		#endregion

		#region List

		public Task<IEnumerable<TProjection>> ListAsync(IDbConnection connection, Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null, IDbTransaction transaction = null)
			=> ListAsync(connection, GetFilter(filterSetup), optionsSetup.Get(), transaction: transaction);

		public Task<IEnumerable<T>> ListAsync<T>(IDbConnection connection, Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null, IDbTransaction transaction = null)
			=> ListAsync<T>(connection, GetFilter(filterSetup), optionsSetup.Get(), transaction: transaction);

		public override sealed Task<IEnumerable<TProjection>> ListAsync(TFilter filter, TOptions options)
			=> _entity.ExecuteAsyncWithTransaction((conn, tran) => ListAsync(conn, filter, options, tran));

		public virtual async Task<IEnumerable<TProjection>> ListAsync(IDbConnection connection, TFilter filter, TOptions options, IDbTransaction transaction = null)
		{
			var models = await _entity.ListAsync(connection, filter, options, transaction);
			if (models == null)
				return default;

			return models.Select(Instance);
		}

		public override sealed Task<IEnumerable<T>> ListAsync<T>(TFilter filter, TOptions options)
			=> _entity.ExecuteAsyncWithTransaction((conn, tran) => ListAsync<T>(conn, filter, options, tran));

		public virtual Task<IEnumerable<T>> ListAsync<T>(IDbConnection connection, TFilter filter, TOptions options, IDbTransaction transaction = null)
		{
			return _entity.ListAsync<T>(connection, filter, options, transaction: transaction);
		}

		#endregion

		#endregion

		#region IAggregate

		#region Max

		public override sealed Task<TResult?> MaxAsync<TResult>(string columName, Action<TFilter> filterSetup)
			=> _entity.ExecuteAsyncWithTransaction((conn, tran) => MaxAsync<TResult>(conn, columName, filterSetup, tran));

		public Task<TResult?> MaxAsync<TResult>(IDbConnection connection, string columnName, IDbTransaction transaction = null) where TResult : struct
			=> MaxAsync<TResult>(connection, columnName, null, transaction: transaction);

		public virtual Task<TResult?> MaxAsync<TResult>(IDbConnection connection, string columnName, Action<TFilter> filterSetup = null, IDbTransaction transaction = null) where TResult : struct
			=> _entity.MaxAsync<TResult>(connection, columnName, filterSetup, transaction: transaction);

		#endregion

		#region Min

		public override sealed Task<TResult?> MinAsync<TResult>(string columName, Action<TFilter> filterSetup)
			=> _entity.ExecuteAsyncWithTransaction((conn, tran) => MinAsync<TResult>(conn, columName, filterSetup, tran));

		public Task<TResult?> MinAsync<TResult>(IDbConnection connection, string columnName, IDbTransaction transaction = null) where TResult : struct
			=> MinAsync<TResult>(connection, columnName, null, transaction: transaction);

		public Task<TResult?> MinAsync<TResult>(IDbConnection connection, string columnName, Action<TFilter> filterSetup = null, IDbTransaction transaction = null) where TResult : struct
			=> _entity.MinAsync<TResult>(connection, columnName, filterSetup, transaction: transaction);

		#endregion

		#endregion

		#region IReplace

		public override sealed Task<(int Deleted, int Added)> ReplaceAsync(Action<TFilter> filterSetup, IEnumerable<TProjection> models)
			=> _entity.ExecuteAsyncWithTransaction((conn, tran) => ReplaceAsync(conn, tran, filterSetup, models));

		public virtual Task<(int Deleted, int Added)> ReplaceAsync(IDbConnection conn, IDbTransaction tran, Action<TFilter> filterSetup, IEnumerable<TProjection> models)
			=> _entity.ReplaceAsync(conn, tran, filterSetup, models.Select(x => x.Model));

		#endregion

		#region IUpdate

		public override sealed Task<int> UpdateAsync(Action<TUpdate> updateSetup, Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null)
			=> _entity.ExecuteAsyncWithTransaction((conn, tran) => UpdateAsync(conn, updateSetup, filterSetup, optionsSetup, tran));

		public Task<int> UpdateAsync(IDbConnection connection, Action<TUpdate> updateSetup, Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null, IDbTransaction transaction = null)
			=> _entity.UpdateAsync(connection, updateSetup, filterSetup, optionsSetup, transaction: transaction);

		public Task<int> UpdateAsync(IEnumerable<TProjection> models, Action<IEnumerable<TProjection>, IEnumerable<TProjection>, IEnumerable<PropertyInfo>, IEnumerable<PropertyInfo>> outputMap)
			=> _entity.ExecuteAsyncWithTransaction((conn, tran) => UpdateAsync(conn, models, outputMap, tran));

		public Task<int> UpdateAsync(IDbConnection connection, IEnumerable<TProjection> models, Action<IEnumerable<TProjection>, IEnumerable<TProjection>, IEnumerable<PropertyInfo>, IEnumerable<PropertyInfo>> outputMap = null, IDbTransaction transaction = null)
			=> _entity.UpdateAsync(connection, models.Select(x => x.Model), OutputMapRedirect(models, outputMap), transaction);

		#endregion

		#region IUpsert

		public Task<int> UpsertAsync(IEnumerable<TProjection> models, Action<IEnumerable<TProjection>, IEnumerable<TProjection>, IEnumerable<PropertyInfo>, IEnumerable<PropertyInfo>> outputMap)
			=> _entity.ExecuteAsyncWithTransaction((conn, tran) => UpsertAsync(conn, models, outputMap, tran));

		public Task<int> UpsertAsync(IDbConnection connection, IEnumerable<TProjection> models, Action<IEnumerable<TProjection>, IEnumerable<TProjection>, IEnumerable<PropertyInfo>, IEnumerable<PropertyInfo>> outputMap = null, IDbTransaction transaction = null)
			=> _entity.UpsertAsync(connection, models.Select(x => x.Model), outputMap: OutputMapRedirect(models, outputMap), transaction: transaction);

		#endregion

		Task<R> IEntity<TProjection, TUpdate, TFilter, TOptions>.ExecuteAsyncWithTransaction<R>(Func<IDbConnection, IDbTransaction, Task<R>> function)
			=> _entity.ExecuteAsyncWithTransaction(function);

		protected Action<IEnumerable<TModel>, IEnumerable<TModel>, IEnumerable<PropertyInfo>, IEnumerable<PropertyInfo>> OutputMapRedirect(
			IEnumerable<TProjection> entitiesOfT,
			Action<IEnumerable<TProjection>, IEnumerable<TProjection>, IEnumerable<PropertyInfo>, IEnumerable<PropertyInfo>> outputMap
		)
		{
			if (outputMap == null)
				return null;

			return (entities, results, propertiesToMatch, propertiesToMap) =>
			{
				outputMap(entitiesOfT, results.Select(Instance), propertiesToMatch, propertiesToMap);
			};
		}
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
