using Dapper.Contrib.Extensions;
using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Simpleverse.Repository.Db.SqlServer;
using Simpleverse.Repository.Db.SqlServer.Merge;
using Simpleverse.Repository.Operations;

namespace Simpleverse.Repository.Db
{
	public class Entity<TModel, TUpdate, TFilter, TOptions> : Entity<TModel, TFilter, TOptions>, IUpdate<TUpdate, TFilter, TOptions>
		where TModel : class
		where TUpdate : class, IFilter, new()
		where TFilter : class, IFilter, new()
		where TOptions : Options, new()
	{
		public Entity(SqlRepository repository, Table<TModel> source)
			: base(repository, source)
		{
		}

		public virtual Task<int> UpdateAsync(Action<TUpdate> updateSetup, Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null)
		{
			var update = updateSetup.Get();
			var filter = filterSetup.Get();
			var options = optionsSetup.Get();

			var builder = Source.AsQuery();
			UpdateQuery(builder, update, filter, options);

			var query = UpdateTemplate(builder, update, filter, options);
			return Repository.ExecuteAsync(query);
		}

		protected virtual void UpdateQuery(QueryBuilder<TModel> builder, TUpdate update, TFilter filter, TOptions options)
		{
			Set(builder, update);
			Query(builder, filter);
		}

		protected virtual SqlBuilder.Template UpdateTemplate(QueryBuilder<TModel> builder, TUpdate update, TFilter filter, TOptions options)
		{
			return builder.AsUpdate();
		}

		protected virtual void Set(QueryBuilder<TModel> builder, TUpdate update)
		{

		}
	}

	public class Entity<TModel, TFilter, TOptions>
		: Entity<TModel>, IQueryExist<TFilter>, IQueryGet<TModel, TFilter, TOptions>, IQueryList<TModel, TFilter, TOptions>, IDelete<TModel, TFilter, TOptions>
		where TModel : class
		where TFilter : class, IFilter, new()
		where TOptions : Options, new()
	{
		public Entity(DbRepository repository, Table<TModel> source)
			: base(repository, source)
		{
		}

		public virtual Task<TModel> GetAsync(Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null)
		{
			return GetAsync<TModel>(filterSetup, optionsSetup);
		}

		public virtual async Task<T> GetAsync<T>(Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null)
		{
			return (await ListAsync<T>(filterSetup, options => { options.Take = 1; optionsSetup?.Invoke(options); })).FirstOrDefault();
		}

		public virtual async Task<bool> ExistsAsync(Action<TFilter> filterSetup = null)
		{
			return await GetAsync(filterSetup: filterSetup) != null;
		}

		public virtual Task<IEnumerable<TModel>> ListAsync(Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null)
		{
			return ListAsync<TModel>(filterSetup, optionsSetup);
		}

		public virtual Task<IEnumerable<T>> ListAsync<T>(Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null)
		{
			var filter = filterSetup.Get();
			var options = optionsSetup.Get();
			return ListAsync<T>(filter, options);
		}

		public virtual Task<IEnumerable<TModel>> ListAsync(TFilter filter, TOptions options)
		{
			return ListAsync<TModel>(filter, options);
		}

		public virtual Task<IEnumerable<T>> ListAsync<T>(TFilter filter, TOptions options)
		{
			var builder = Source.AsQuery();
			SelectQuery(builder, filter, options);

			var query = SelectTemplate(builder, options);
			return Repository.QueryAsync<T>(query);
		}

		protected virtual void SelectQuery(QueryBuilder<TModel> builder, TFilter filter, TOptions options)
			=> Query(builder, filter);

		protected virtual SqlBuilder.Template SelectTemplate(QueryBuilder<TModel> builder, TOptions options)
		{
			builder.Select($"{Source.Alias}.*");
			return builder.AsSelect(options: options);
		}

		public virtual Task<int> DeleteAsync(Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null)
		{
			var filter = filterSetup.Get();
			var options = optionsSetup.Get();
			var builder = Source.AsQuery();
			DeleteQuery(builder, filter, options);

			var query = DeleteTemplate(builder, options);
			return Repository.ExecuteAsync(query);
		}

		protected virtual void DeleteQuery(QueryBuilder<TModel> builder, TFilter filter, TOptions options)
			=> Query(builder, filter);

		protected void Query(QueryBuilder<TModel> builder, TFilter filter)
		{
			Join(builder, filter);
			Filter(builder, filter);
		}

		protected virtual SqlBuilder.Template DeleteTemplate(QueryBuilder<TModel> builder, TOptions options)
		{
			return builder.AsDelete();
		}

		protected virtual void Filter(QueryBuilder<TModel> builder, TFilter filter) { }

		protected virtual void Join(QueryBuilder<TModel> builder, TFilter filter) { }

		public virtual Task<TResult?> MinAsync<TResult>(string columnName, Action<TFilter> filterSetup)
			where TResult : struct
		{
			var builder = Source.AsQuery();
			var query = builder.AddTemplate($@"
				SELECT {Source.Column(columnName).Min()}
				FROM
					{Source}
					/**join**/
					/**innerjoin**/
					/**leftjoin**/
					/**rightjoin**/
				/**where**/
			");

			Filter(builder, filterSetup.Get());
			return Repository.ExecuteAsync((conn, tran) => conn.QueryFirstOrDefaultAsync<TResult?>(query.RawSql, query.Parameters));
		}

		public virtual Task<TResult?> MaxAsync<TResult>(string columnName, Action<TFilter> filterSetup)
			where TResult : struct
		{
			var builder = new QueryBuilder<TModel>();
			Filter(builder, filterSetup.Get());

			var query = builder.AddTemplate($@"
					SELECT {Source.Column(columnName).Max()}
					FROM
						{Source}
						/**join**/
						/**innerjoin**/
						/**leftjoin**/
						/**rightjoin**/
					/**where**/
			"
			);
			return Repository.ExecuteAsync((conn, tran) => conn.QueryFirstOrDefaultAsync<TResult?>(query.RawSql, query.Parameters));
		}
	}

	public class Entity<T> : IAdd<T>, IUpdate<T>, IDelete<T>, IAggregate
		where T : class
	{
		protected DbRepository Repository { get; }
		protected Table<T> Source { get; }

		public Entity(DbRepository repository, Table<T> source)
		{
			Repository = repository;
			Source = source;
		}

		public virtual async Task<T> GetAsync(dynamic id)
		{
			return await Repository.ExecuteAsync<T>((conn, tran) => SqlMapperExtensions.GetAsync<T>(conn, id, transaction: tran));
		}
		public virtual async Task<int> AddAsync(T model)
		{
			return await Repository.ExecuteAsync((conn, tran) => conn.InsertAsync(model, transaction: tran));
		}
		public virtual async Task<int> AddAsync(IEnumerable<T> models, bool mapGeneratedValues = false)
		{
			return await Repository.ExecuteAsyncWithTransaction(
				(conn, tran) =>
				{
					if (Repository is SqlRepository)
					{
						return conn.InsertBulkAsync(
							models,
							transaction: tran,
							mapGeneratedValues: mapGeneratedValues
						);
					}

					return conn.InsertAsync(models, transaction: tran);
				}
			);
		}
		public virtual async Task<bool> UpdateAsync(T model)
		{
			return await Repository.ExecuteAsync((conn, tran) => conn.UpdateAsync(model, transaction: tran));
		}
		public virtual async Task<int> UpdateAsync(IEnumerable<T> models, bool mapGeneratedValues = false)
		{
			return await Repository.ExecuteAsyncWithTransaction(
				async (conn, tran) =>
				{
					if (Repository is SqlRepository)
					{
						return await conn.UpdateBulkAsync(
							models,
							transaction: tran,
							mapGeneratedValues: mapGeneratedValues
						);
					}

					var sucess = await conn.UpdateAsync(models, transaction: tran);
					return models.Count();
				}
			);
		}
		public virtual async Task<int> UpsertAsync(T model)
		{
			return await Repository.ExecuteAsync(
				(conn, tran) =>
				{
					if (Repository is SqlRepository)
						return conn.UpsertAsync(model, transaction: tran);

					throw new NotSupportedException("Upsert is not supported on non-SQL server connections.");
				}
			);
		}
		public virtual async Task<int> UpsertAsync(IEnumerable<T> models)
		{
			return await Repository.ExecuteAsyncWithTransaction(
				(conn, tran) =>
				{
					if (Repository is SqlRepository)
						return conn.UpsertBulkAsync(models, transaction: tran);

					throw new NotSupportedException("Upsert is not supported on non-SQL server connections.");
				}
			);
		}
		public virtual async Task<bool> DeleteAsync(T model)
		{
			return await Repository.ExecuteAsync((conn, tran) => conn.DeleteAsync(model, transaction: tran));
		}
		public virtual async Task<int> DeleteAsync(IEnumerable<T> models)
		{
			return await Repository.ExecuteAsyncWithTransaction(
				async (conn, tran) =>
				{
					if (Repository is SqlRepository)
					{
						return await conn.DeleteBulkAsync(
							models,
							transaction: tran
						);
					}

					var sucess = await conn.DeleteAsync(models, transaction: tran);
					return models.Count();
				}
			);
		}
		public virtual Task<TResult?> MinAsync<TResult>(string columnName)
			where TResult : struct
		{
			return MinAsync<TResult>(Source.Column(columnName));
		}
		public virtual Task<TResult?> MinAsync<TResult>(Expression<Func<T, TResult>> columnExpression)
			where TResult : struct
		{
			return MinAsync<TResult>(Source.Column(columnExpression));
		}
		protected virtual Task<TResult?> MinAsync<TResult>(Selector column)
			where TResult : struct
		{
			return Repository.ExecuteAsync((conn, tran) => conn.QueryFirstOrDefaultAsync<TResult?>($"SELECT {column.Min()} FROM {Source}"));
		}
		public virtual Task<TResult?> MaxAsync<TResult>(string columnName)
			where TResult : struct
		{
			return MaxAsync<TResult>(Source.Column(columnName));
		}
		public virtual Task<TResult?> MaxAsync<TResult>(Expression<Func<T, TResult>> columnExpression)
			where TResult : struct
		{
			return MaxAsync<TResult>(Source.Column(columnExpression));
		}
		protected virtual Task<TResult?> MaxAsync<TResult>(Selector column)
			where TResult : struct
		{
			return Repository.ExecuteAsync((conn, tran) => conn.QueryFirstOrDefaultAsync<TResult?>($"SELECT {column.Max()} FROM {Source}"));
		}
	}
}
