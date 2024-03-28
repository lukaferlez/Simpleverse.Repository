using Dapper;
using Dapper.Contrib.Extensions;
using Simpleverse.Repository.Db.Operations;
using Simpleverse.Repository.Db.SqlServer;
using Simpleverse.Repository.Db.SqlServer.Merge;
using Simpleverse.Repository.Operations;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace Simpleverse.Repository.Db
{
	public class Entity<TModel, TUpdate, TFilter, TOptions> : Entity<TModel, TFilter, TOptions>, IUpdate<TUpdate, TFilter, TOptions>
		where TModel : class
		where TUpdate : class, IQueryFilter, new()
		where TFilter : class, IQueryFilter, new()
		where TOptions : DbQueryOptions, new()
	{
		public Entity(DbRepository repository, Table<TModel> source)
			: base(repository, source)
		{
		}

		public virtual Task<int> UpdateAsync(Action<TUpdate> updateSetup, Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null)
			=> Repository.ExecuteAsync((conn, tran) => UpdateAsync(conn, updateSetup, filterSetup, optionsSetup, tran));

		public virtual Task<int> UpdateAsync(IDbConnection conn, Action<TUpdate> updateSetup, Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null, IDbTransaction tran = null)
		{
			var update = updateSetup.Get();
			var filter = filterSetup.Get();
			var options = optionsSetup.Get();

			var builder = Source.AsQuery();
			UpdateQuery(builder, update, filter, options);

			var query = UpdateTemplate(builder, update, filter, options);
			return conn.ExecuteAsync(query, tran: tran);
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
			if (update is UpdateOptions<TModel> updateOptions)
				updateOptions.Apply(builder);
		}
	}

	public class Entity<TModel, TFilter, TOptions>
		: Entity<TModel>,
			IQueryExistDb<TFilter>,
			IQueryGetDb<TModel, TFilter, TOptions>,
			IQueryListDb<TModel, TFilter, TOptions>,
			IDeleteDb<TModel, TFilter, TOptions>
		where TModel : class
		where TFilter : class, IQueryFilter, new()
		where TOptions : DbQueryOptions, new()
	{
		public Entity(DbRepository repository, Table<TModel> source)
			: base(repository, source)
		{
		}

		#region Get

		public Task<TModel> GetAsync(Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null)
			=> GetAsync<TModel>(filterSetup, optionsSetup);
		public Task<TModel> GetAsync(
			IDbConnection connection,
			Action<TFilter> filterSetup = null,
			Action<TOptions> optionsSetup = null,
			IDbTransaction transaction = null
		)
			=> GetAsync<TModel>(connection, filterSetup, optionsSetup, transaction: transaction);

		public async Task<T> GetAsync<T>(Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null)
			=> (await ListAsync<T>(filterSetup, options => { options.Take = 1; optionsSetup?.Invoke(options); })).FirstOrDefault();
		public async Task<T> GetAsync<T>(IDbConnection connection, Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null, IDbTransaction transaction = null)
			=> (await ListAsync<T>(connection, filterSetup, options => { options.Take = 1; optionsSetup?.Invoke(options); }, transaction: transaction)).FirstOrDefault();

		#endregion

		#region Exists

		public async Task<bool> ExistsAsync(Action<TFilter> filterSetup = null)
			=> await GetAsync(filterSetup: filterSetup) != null;
		public async Task<bool> ExistsAsync(IDbConnection connection, Action<TFilter> filterSetup = null, IDbTransaction transaction = null)
			=> await GetAsync(connection, filterSetup: filterSetup, transaction: transaction) != null;

		#endregion

		#region List

		public Task<IEnumerable<TModel>> ListAsync(Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null)
			=> ListAsync<TModel>(filterSetup, optionsSetup);
		public Task<IEnumerable<TModel>> ListAsync(
			IDbConnection connection,
			Action<TFilter> filterSetup = null,
			Action<TOptions> optionsSetup = null,
			IDbTransaction transaction = null
		)
			=> ListAsync<TModel>(connection, filterSetup, optionsSetup, transaction: transaction);

		public Task<IEnumerable<T>> ListAsync<T>(Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null)
		{
			var filter = filterSetup.Get();
			var options = optionsSetup.Get();
			return ListAsync<T>(filter, options);
		}
		public Task<IEnumerable<T>> ListAsync<T>(
			IDbConnection connection,
			Action<TFilter> filterSetup = null,
			Action<TOptions> optionsSetup = null,
			IDbTransaction transaction = null
		)
		{
			var filter = filterSetup.Get();
			var options = optionsSetup.Get();
			return ListAsync<T>(connection, filter, options, transaction: transaction);
		}

		public Task<IEnumerable<TModel>> ListAsync(TFilter filter, TOptions options)
			=> ListAsync<TModel>(filter, options);
		public Task<IEnumerable<TModel>> ListAsync(IDbConnection connection, TFilter filter, TOptions options, IDbTransaction transaction = null)
			=> ListAsync<TModel>(connection, filter, options, transaction);

		public virtual Task<IEnumerable<T>> ListAsync<T>(TFilter filter, TOptions options)
			=> Repository.ExecuteAsync((conn, tran) => ListAsync<T>(conn, filter, options, transaction: tran));
		public virtual Task<IEnumerable<T>> ListAsync<T>(IDbConnection connection, TFilter filter, TOptions options, IDbTransaction transaction = null)
		{
			var builder = Source.AsQuery();

			SelectQuery(builder, filter, options);
			var query = SelectTemplate(builder, options);

			var type = typeof(T);
			if (type.Name.StartsWith("ValueTuple`"))
			{
				var tupleTypeArguments = type.GenericTypeArguments;
				var typeArgumentsCount = tupleTypeArguments.Count();
				if (typeArgumentsCount > 7)
					throw new NotSupportedException("Number of Tuple arguments is more than the supported 7.");

				return (Task<IEnumerable<T>>)
					typeof(DbConnectionExtensions)
					.GetMethod(nameof(DbConnectionExtensions.QueryAsync), typeArgumentsCount, new[] { typeof(IDbConnection), query.GetType(), typeof(IDbTransaction) })
					.MakeGenericMethod(type.GenericTypeArguments)
					.Invoke(null, new object[] { connection, query, transaction });
			}

			return connection.QueryAsync<T>(query, tran: transaction);
		}

		protected virtual void SelectQuery(QueryBuilder<TModel> builder, TFilter filter, TOptions options)
		{
			builder.SelectAll();
			Query(builder, filter);
		}

		protected virtual SqlBuilder.Template SelectTemplate(QueryBuilder<TModel> builder, TOptions options)
		{
			return builder.AsSelect(options: options);
		}

		#endregion

		#region Delete

		public Task<int> DeleteAsync(Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null)
			=> Repository.ExecuteAsync((conn, tran) => DeleteAsync(conn, filterSetup, optionsSetup, transaction: tran));
		public virtual Task<int> DeleteAsync(
			IDbConnection connection,
			Action<TFilter> filterSetup = null,
			Action<TOptions> optionsSetup = null,
			IDbTransaction transaction = null
		)
		{
			var filter = filterSetup.Get();
			var options = optionsSetup.Get();
			var builder = Source.AsQuery();
			DeleteQuery(builder, filter, options);

			var query = DeleteTemplate(builder, options);
			return connection.ExecuteAsync(query, tran: transaction);
		}

		protected virtual void DeleteQuery(QueryBuilder<TModel> builder, TFilter filter, TOptions options)
			=> Query(builder, filter);

		protected virtual SqlBuilder.Template DeleteTemplate(QueryBuilder<TModel> builder, TOptions options)
		{
			return builder.AsDelete();
		}

		#endregion

		protected void Query(QueryBuilder<TModel> builder, TFilter filter)
		{
			Join(builder, filter);
			Filter(builder, filter);
		}

		protected virtual void Filter(QueryBuilder<TModel> builder, TFilter filter) { }

		protected virtual void Join(QueryBuilder<TModel> builder, TFilter filter) { }

		#region Min

		public virtual Task<TResult?> MinAsync<TResult>(string columnName, Action<TFilter> filterSetup)
			where TResult : struct
		{
			return Repository.ExecuteAsync(
				(conn, tran) => MinAsync<TResult>(conn, columnName, filterSetup, transaction: tran)
			);
		}

		public virtual Task<TResult?> MinAsync<TResult>(
			IDbConnection connection,
			string columnName,
			Action<TFilter> filterSetup,
			IDbTransaction transaction = null
		)
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

		#endregion

		#region Max

		public virtual Task<TResult?> MaxAsync<TResult>(string columnName, Action<TFilter> filterSetup)
			where TResult : struct
		{
			return Repository.ExecuteAsync(
				(conn, tran) => MaxAsync<TResult>(conn, columnName, filterSetup, transaction: tran)
			);
		}

		public virtual Task<TResult?> MaxAsync<TResult>(
			IDbConnection connection,
			string columnName,
			Action<TFilter> filterSetup,
			IDbTransaction transaction = null
		)
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
			return connection.QueryFirstOrDefaultAsync<TResult?>(query.RawSql, query.Parameters, transaction: transaction);
		}

		#endregion
	}

	public class Entity<T> : IAddDb<T>, IUpdateDb<T>, IDeleteDb<T>, IAggregateDb
		where T : class
	{
		protected DbRepository Repository { get; }
		protected Table<T> Source { get; }

		public Entity(DbRepository repository, Table<T> source)
		{
			Repository = repository;
			Source = source;
		}

		#region Get

		public async Task<T> GetAsync(dynamic id)
		{
			return await Repository.ExecuteAsync<T>((conn, tran) => GetAsync(conn, id, transaction: tran));
		}
		public virtual Task<T> GetAsync(IDbConnection connection, dynamic id, IDbTransaction transaction = null)
			=> SqlMapperExtensions.GetAsync<T>(connection, id, transaction: transaction);

		#endregion

		#region Add

		public async Task<int> AddAsync(T model)
		{
			return await Repository.ExecuteAsync((conn, tran) => AddAsync(conn, model, transaction: tran));
		}
		public virtual Task<int> AddAsync(IDbConnection connection, T model, IDbTransaction transaction = null)
			=> connection.InsertAsync(model, transaction: transaction);

		public async Task<int> AddAsync(
			IEnumerable<T> models,
			Action<IEnumerable<T>, IEnumerable<T>, IEnumerable<PropertyInfo>, IEnumerable<PropertyInfo>> outputMap = null
		)
		{
			return await Repository.ExecuteAsyncWithTransaction(
				(conn, tran) => AddAsync(conn, models, outputMap: outputMap, transaction: tran)
			);
		}
		public virtual Task<int> AddAsync(
			IDbConnection connection,
			IEnumerable<T> models,
			Action<IEnumerable<T>, IEnumerable<T>, IEnumerable<PropertyInfo>, IEnumerable<PropertyInfo>> outputMap = null,
			IDbTransaction transaction = null
		)
		{
			if (Repository is SqlRepository)
			{
				return connection.InsertBulkAsync(
					models,
					transaction: transaction,
					outputMap: outputMap
				);
			}

			return connection.InsertAsync(models, transaction: transaction);
		}

		#endregion

		#region Update

		public async Task<bool> UpdateAsync(T model)
		{
			return await Repository.ExecuteAsync((conn, tran) => UpdateAsync(conn, model, transaction: tran));
		}
		public virtual Task<bool> UpdateAsync(IDbConnection connection, T model, IDbTransaction transaction = null)
			=> connection.UpdateAsync(model, transaction: transaction);

		public async Task<int> UpdateAsync(
			IEnumerable<T> models,
			Action<IEnumerable<T>, IEnumerable<T>, IEnumerable<PropertyInfo>, IEnumerable<PropertyInfo>> outputMap = null
		)
		{
			return await Repository.ExecuteAsyncWithTransaction(
				(conn, tran) => UpdateAsync(conn, models, outputMap: outputMap, transaction: tran)
			);
		}
		public virtual async Task<int> UpdateAsync(
			IDbConnection connection,
			IEnumerable<T> models,
			Action<IEnumerable<T>, IEnumerable<T>, IEnumerable<PropertyInfo>, IEnumerable<PropertyInfo>> outputMap = null,
			IDbTransaction transaction = null
		)
		{
			if (Repository is SqlRepository)
			{
				return await connection.UpdateBulkAsync(
					models,
					transaction: transaction,
					outputMap: outputMap
				);
			}

			var sucess = await connection.UpdateAsync(models, transaction: transaction);
			return models.Count();
		}

		#endregion

		#region Upsert

		public async Task<int> UpsertAsync(T model)
		{
			return await Repository.ExecuteAsync(
				(conn, tran) => UpsertAsync(conn, model, transaction: tran)
			);
		}
		public virtual Task<int> UpsertAsync(IDbConnection connection, T model, IDbTransaction transaction = null)
		{
			if (!(Repository is SqlRepository))
				throw new NotSupportedException("Upsert is not supported on non-SQL repository connections.");

			return connection.UpsertAsync(model, transaction: transaction);
		}

		public async Task<int> UpsertAsync(
			IEnumerable<T> models,
			Action<IEnumerable<T>, IEnumerable<T>, IEnumerable<PropertyInfo>, IEnumerable<PropertyInfo>> outputMap = null
		)
		{
			return await Repository.ExecuteAsyncWithTransaction(
				(conn, tran) => UpsertAsync(conn, models, outputMap: outputMap, transaction: tran)
			);
		}
		public virtual Task<int> UpsertAsync(
			IDbConnection connection,
			IEnumerable<T> models,
			Action<IEnumerable<T>, IEnumerable<T>, IEnumerable<PropertyInfo>, IEnumerable<PropertyInfo>> outputMap = null,
			IDbTransaction transaction = null
		)
		{
			if (!(Repository is SqlRepository))
				throw new NotSupportedException("Upsert is not supported on non-SQL repository connections.");

			return connection.UpsertBulkAsync(models, transaction: transaction);
		}

		#endregion

		#region Delete

		public async Task<bool> DeleteAsync(T model)
		{
			return await Repository.ExecuteAsync((conn, tran) => DeleteAsync(conn, model, transaction: tran));
		}
		public virtual Task<bool> DeleteAsync(IDbConnection connection, T model, IDbTransaction transaction = null)
			=> connection.DeleteAsync(model, transaction: transaction);

		public async Task<int> DeleteAsync(IEnumerable<T> models)
		{
			return await Repository.ExecuteAsyncWithTransaction(
				(conn, tran) => DeleteAsync(conn, models, transaction: tran)
			);
		}
		public virtual async Task<int> DeleteAsync(IDbConnection connection, IEnumerable<T> models, IDbTransaction transaction = null)
		{
			if (Repository is SqlRepository)
			{
				return await connection.DeleteBulkAsync(
					models,
					transaction: transaction
				);
			}

			var sucess = await connection.DeleteAsync(models, transaction: transaction);
			return models.Count();
		}

		#endregion

		#region Min

		public Task<TResult?> MinAsync<TResult>(string columnName)
			where TResult : struct
			=> MinAsync<TResult>(Source.Column(columnName));
		public Task<TResult?> MinAsync<TResult>(IDbConnection connection, string columnName, IDbTransaction transaction = null)
			where TResult : struct
			=> MinAsync<TResult>(connection, Source.Column(columnName), transaction: transaction);

		public Task<TResult?> MinAsync<TResult>(Expression<Func<T, TResult>> columnExpression)
			where TResult : struct
			=> MinAsync<TResult>(Source.Column(columnExpression));
		public Task<TResult?> MinAsync<TResult>(IDbConnection connection, Expression<Func<T, TResult>> columnExpression, IDbTransaction transaction = null)
			where TResult : struct
			=> MinAsync<TResult>(connection, Source.Column(columnExpression), transaction: transaction);

		protected virtual Task<TResult?> MinAsync<TResult>(Selector column)
			where TResult : struct
			=> Repository.ExecuteAsync((conn, tran) => MinAsync<TResult>(conn, column, transaction: tran));
		protected virtual Task<TResult?> MinAsync<TResult>(IDbConnection connection, Selector column, IDbTransaction transaction = null)
			where TResult : struct
			=> connection.QueryFirstOrDefaultAsync<TResult?>($"SELECT {column.Min()} FROM {Source}", transaction: transaction);

		#endregion

		#region Max

		public Task<TResult?> MaxAsync<TResult>(string columnName)
			where TResult : struct
			=> MaxAsync<TResult>(Source.Column(columnName));
		public Task<TResult?> MaxAsync<TResult>(IDbConnection connection, string columnName, IDbTransaction transaction = null)
			where TResult : struct
			=> MaxAsync<TResult>(connection, Source.Column(columnName), transaction: transaction);

		public Task<TResult?> MaxAsync<TResult>(Expression<Func<T, TResult>> columnExpression)
			where TResult : struct
			=> MaxAsync<TResult>(Source.Column(columnExpression));
		public Task<TResult?> MaxAsync<TResult>(IDbConnection connection, Expression<Func<T, TResult>> columnExpression, IDbTransaction transaction = null)
			where TResult : struct
			=> MaxAsync<TResult>(connection, Source.Column(columnExpression), transaction);

		protected virtual Task<TResult?> MaxAsync<TResult>(Selector column)
			where TResult : struct
			=> Repository.ExecuteAsync((conn, tran) => MaxAsync<TResult>(conn, column, transaction: tran));
		protected virtual Task<TResult?> MaxAsync<TResult>(IDbConnection connection, Selector column, IDbTransaction transaction = null)
			where TResult : struct
			=> connection.QueryFirstOrDefaultAsync<TResult?>($"SELECT {column.Max()} FROM {Source}", transaction: transaction);

		#endregion
	}
}
