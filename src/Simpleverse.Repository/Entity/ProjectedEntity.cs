using Simpleverse.Repository.ChangeTracking;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Simpleverse.Repository.Entity
{
	public class ProjectedEntity<TProjection, TEntity, TModel, TUpdate, TFilter, TOptions>
		: IProjectedEntity<TProjection, TModel, TUpdate, TFilter, TOptions>
		where TProjection : class, IProject<TModel>
		where TEntity : IEntity<TModel, TUpdate, TFilter, TOptions>
		where TModel : class, new()
		where TFilter : class
		where TUpdate : class
		where TOptions : class, new()
	{
		protected ConstructorInfo _constructorMethod;
		protected readonly Func<TModel, TProjection> _creator;
		protected readonly TEntity _entity;

		public ProjectedEntity(TEntity entity)
		{
			_entity = entity;
		}

		public ProjectedEntity(TEntity entity, Func<TModel, TProjection> creator)
			: this(entity)
		{
			_creator = creator;
		}

		#region IAdd

		public Task<int> AddAsync(TProjection model, CancellationToken cancellationToken = default)
			=> AddAsync(new[] { model }, cancellationToken);

		public virtual Task<int> AddAsync(IEnumerable<TProjection> models, CancellationToken cancellationToken = default)
			=> _entity.AddAsync(models.Select(x => x.Model), cancellationToken);

		#endregion

		#region IDelete

		public virtual Task<int> DeleteAsync(Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null, CancellationToken cancellationToken = default)
			=> _entity.DeleteAsync(filterSetup, optionsSetup, cancellationToken);

		public async Task<bool> DeleteAsync(TProjection model)
			=> await DeleteAsync(new[] { model }) > 0;

		public virtual Task<int> DeleteAsync(IEnumerable<TProjection> models, CancellationToken cancellationToken = default)
			=> _entity.DeleteAsync(models.Select(x => x.Model), cancellationToken);

		#endregion

		#region IQuery

		public virtual async Task<bool> ExistsAsync(Action<TFilter> filterSetup = null, CancellationToken cancellationToken = default)
			 => await GetAsync(filterSetup, null, cancellationToken) != null;

		#region Get

		public async Task<TProjection> GetAsync(Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null, CancellationToken cancellationToken = default)
		{
			var model = await GetAsync<TModel>(filterSetup, optionsSetup, cancellationToken);
			if (model == null)
				return null;

			return Instance(model);
		}

		public virtual async Task<T> GetAsync<T>(Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null, CancellationToken cancellationToken = default)
			=> (await ListAsync<T>(filterSetup, optionsSetup, cancellationToken)).FirstOrDefault();

		#endregion

		#region List

		public Task<IEnumerable<TProjection>> ListAsync(Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null, CancellationToken cancellationToken = default)
			=> ListAsync(GetFilter(filterSetup), optionsSetup.Get(), cancellationToken);

		public Task<IEnumerable<T>> ListAsync<T>(Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null, CancellationToken cancellationToken = default)
			=> ListAsync<T>(GetFilter(filterSetup), optionsSetup.Get(), cancellationToken);

		public virtual async Task<IEnumerable<TProjection>> ListAsync(TFilter filter, TOptions options, CancellationToken cancellationToken = default)
		{
			var models = await _entity.ListAsync(filter, options, cancellationToken);
			if (models == null)
				return default;

			return models.Select(Instance);
		}

		public virtual Task<IEnumerable<T>> ListAsync<T>(TFilter filter, TOptions options, CancellationToken cancellationToken = default)
			=> _entity.ListAsync<T>(filter, options, cancellationToken);

		#endregion

		#endregion

		#region IAggregate

		#region Max

		public Task<TResult?> MaxAsync<TResult>(string columnName, CancellationToken cancellationToken = default) where TResult : struct
			=> MaxAsync<TResult>(columnName, null, cancellationToken);

		public virtual Task<TResult?> MaxAsync<TResult>(string columName, Action<TFilter> filterSetup, CancellationToken cancellationToken = default) where TResult : struct
			=> _entity.MaxAsync<TResult>(columName, filterSetup, cancellationToken);

		#endregion

		#region Min

		public Task<TResult?> MinAsync<TResult>(string columnName, CancellationToken cancellationToken = default) where TResult : struct
			=> MinAsync<TResult>(columnName, null, cancellationToken);

		public virtual Task<TResult?> MinAsync<TResult>(string columName, Action<TFilter> filterSetup, CancellationToken cancellationToken = default) where TResult : struct
			=> _entity.MinAsync<TResult>(columName, filterSetup, cancellationToken);

		#endregion

		#endregion

		#region IDelete

		public virtual Task<(int Deleted, int Added)> ReplaceAsync(Action<TFilter> filterSetup, IEnumerable<TProjection> models, CancellationToken cancellationToken = default)
			=> _entity.ReplaceAsync(filterSetup, models.Select(x => x.Model), cancellationToken);

		#endregion

		#region IUpdate

		public virtual Task<int> UpdateAsync(Action<TUpdate> updateSetup, Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null, CancellationToken cancellationToken = default)
			=> _entity.UpdateAsync(updateSetup, filterSetup, optionsSetup, cancellationToken);

		public Task<int> UpdateAsync(TProjection model, CancellationToken cancellationToken = default)
			=> UpdateAsync(new[] { model }, cancellationToken);

		public virtual Task<int> UpdateAsync(IEnumerable<TProjection> models, CancellationToken cancellationToken = default)
			=> _entity.UpdateAsync(models.Select(x => x.Model), cancellationToken);

		#endregion

		#region IUpsert

		public Task<int> UpsertAsync(TProjection model, CancellationToken cancellationToken = default)
			=> UpsertAsync(new[] { model }, cancellationToken);

		public virtual Task<int> UpsertAsync(IEnumerable<TProjection> models, CancellationToken cancellationToken = default)
			=> _entity.UpsertAsync(models.Select(x => x.Model), cancellationToken);

		#endregion

		protected TProjection Instance(TModel model)
		{
			if (_creator != null)
				return _creator(model);

			if (_constructorMethod == null)
				_constructorMethod = typeof(TProjection).GetConstructor(new[] { typeof(TModel) });

			return (TProjection)_constructorMethod.Invoke(new[] { model });
		}

		protected TFilter GetFilter(Action<TFilter> filterSetup)
		{
			return filterSetup.Get(
				() => ChangeProxyFactory.Create<TFilter>()
			);
		}
	}

	public class ProjectedEntity<TProjection, TModel, TUpdate, TFilter, TOptions>
		: ProjectedEntity<TProjection, IEntity<TModel, TUpdate, TFilter, TOptions>, TModel, TUpdate, TFilter, TOptions>
		where TProjection : class, IProject<TModel>
		where TModel : class, new()
		where TFilter : class
		where TUpdate : class
		where TOptions : class, new()
	{
		public ProjectedEntity(IEntity<TModel, TUpdate, TFilter, TOptions> entity)
			: base(entity)
		{
		}

		public ProjectedEntity(IEntity<TModel, TUpdate, TFilter, TOptions> entity, Func<TModel, TProjection> creator)
			: base(entity, creator)
		{
		}
	}

	public class ProjectedEntity<TProjection, TModel, TFilter, TOptions>
		: ProjectedEntity<TProjection, TModel, TModel, TFilter, TOptions>, IProjectedEntity<TProjection, TModel, TFilter, TOptions>
		where TProjection : class, IProject<TModel>
		where TModel : class, new()
		where TFilter : class
		where TOptions : class, new()
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
		where TOptions : class, new()
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
		: ProjectedEntity<TProjection, TModel, QueryOptions>, IProjectedEntity<TProjection, TModel>
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
