using Dapper;
using Dapper.Contrib.Extensions;
using Simpleverse.Repository.ChangeTracking;
using Simpleverse.Repository.Db.Extensions.Dapper;
using Simpleverse.Repository.Db.Meta;
using Simpleverse.Repository.Db.SqlServer;
using Simpleverse.Repository.Db.SqlServer.Merge;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Simpleverse.Repository.Db.Entity
{
	public class Entity<TModel, TUpdate, TFilter, TOptions>
		: IEntity<TModel, TUpdate, TFilter, TOptions>, Repository.Entity.IEntity<TModel, TUpdate, TFilter, TOptions>
		where TModel : class
		where TFilter : class
		where TUpdate : class
		where TOptions : DbQueryOptions, new()
	{
		protected DbRepository Repository { get; }
		protected Table<TModel> Source { get; }

		public Entity(DbRepository repository, Table<TModel> source)
		{
			Repository = repository;
			Source = source;
		}

		#region IQuery

		#region Get

		public async Task<TModel> GetAsync(dynamic id)
			=> await Repository.ExecuteAsync<TModel>((conn) => GetAsync(conn, id));

		public virtual Task<TModel> GetAsync(IDbConnection connection, dynamic id, IDbTransaction transaction = null)
			=> SqlMapperExtensions.GetAsync<TModel>(connection, id, transaction: transaction);

		public Task<TModel> GetAsync(Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null, CancellationToken cancellationToken = default)
			=> GetAsync<TModel>(filterSetup, optionsSetup, cancellationToken);
		public Task<TModel> GetAsync(
			IDbConnection connection,
			Action<TFilter> filterSetup = null,
			Action<TOptions> optionsSetup = null,
			IDbTransaction transaction = null,
			CancellationToken cancellationToken = default
		)
			=> GetAsync<TModel>(connection, filterSetup, optionsSetup, transaction: transaction, cancellationToken: cancellationToken);

		public async Task<T> GetAsync<T>(Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null, CancellationToken cancellationToken = default)
			=> (await ListAsync<T>(filterSetup, options => { options.Take = 1; optionsSetup?.Invoke(options); }, cancellationToken)).FirstOrDefault();
		public async Task<T> GetAsync<T>(IDbConnection connection, Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null, IDbTransaction transaction = null, CancellationToken cancellationToken = default)
			=> (await ListAsync<T>(connection, filterSetup, options => { options.Take = 1; optionsSetup?.Invoke(options); }, transaction: transaction, cancellationToken: cancellationToken)).FirstOrDefault();

		#endregion

		#region Exists

		public async Task<bool> ExistsAsync(Action<TFilter> filterSetup = null, CancellationToken cancellationToken = default)
			=> await GetAsync(filterSetup: filterSetup, cancellationToken: cancellationToken) != null;
		public async Task<bool> ExistsAsync(IDbConnection connection, Action<TFilter> filterSetup = null, IDbTransaction transaction = null, CancellationToken cancellationToken = default)
			=> await GetAsync(connection, filterSetup: filterSetup, transaction: transaction, cancellationToken: cancellationToken) != null;

		#endregion

		#region List

		public Task<IEnumerable<TModel>> ListAsync(Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null, CancellationToken cancellationToken = default)
			=> ListAsync<TModel>(filterSetup, optionsSetup, cancellationToken);
		public Task<IEnumerable<TModel>> ListAsync(
			IDbConnection connection,
			Action<TFilter> filterSetup = null,
			Action<TOptions> optionsSetup = null,
			IDbTransaction transaction = null,
			CancellationToken cancellationToken = default
		)
			=> ListAsync<TModel>(connection, filterSetup, optionsSetup, transaction: transaction, cancellationToken: cancellationToken);

		public Task<IEnumerable<T>> ListAsync<T>(Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null, CancellationToken cancellationToken = default)
		{
			var filter = GetFilter(filterSetup);
			var options = optionsSetup.Get();
			return ListAsync<T>(filter, options, cancellationToken);
		}
		public Task<IEnumerable<T>> ListAsync<T>(
			IDbConnection connection,
			Action<TFilter> filterSetup = null,
			Action<TOptions> optionsSetup = null,
			IDbTransaction transaction = null,
			CancellationToken cancellationToken = default
		)
		{
			var filter = GetFilter(filterSetup);
			var options = optionsSetup.Get();
			return ListAsync<T>(connection, filter, options, transaction: transaction, cancellationToken: cancellationToken);
		}

		public Task<IEnumerable<TModel>> ListAsync(TFilter filter, TOptions options, CancellationToken cancellationToken = default)
			=> ListAsync<TModel>(filter, options, cancellationToken);
		public Task<IEnumerable<TModel>> ListAsync(IDbConnection connection, TFilter filter, TOptions options, IDbTransaction transaction = null, CancellationToken cancellationToken = default)
			=> ListAsync<TModel>(connection, filter, options, transaction, cancellationToken);

		public Task<IEnumerable<T>> ListAsync<T>(TFilter filter, TOptions options, CancellationToken cancellationToken = default)
			=> Repository.ExecuteAsync((conn) => ListAsync<T>(conn, filter, options, cancellationToken: cancellationToken));

		public virtual Task<IEnumerable<T>> ListAsync<T>(IDbConnection connection, TFilter filter, TOptions options, IDbTransaction transaction = null, CancellationToken cancellationToken = default)
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
					.GetMethod(nameof(DbConnectionExtensions.QueryAsync), typeArgumentsCount, new[] { typeof(IDbConnection), query.GetType(), typeof(IDbTransaction), typeof(CancellationToken) })
					.MakeGenericMethod(type.GenericTypeArguments)
					.Invoke(null, new object[] { connection, query, transaction, cancellationToken });
			}

			return connection.QueryAsync<T>(query, tran: transaction, cancellationToken: cancellationToken);
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

		protected void Query(QueryBuilder<TModel> builder, TFilter filter)
		{
			Join(builder, filter);
			Filter(builder, filter);
		}

		#endregion

		#region Add

		public Task<int> AddAsync(TModel model, CancellationToken cancellationToken = default)
			=> AddAsync(new[] { model }, cancellationToken);

		public Task<int> AddAsync(IEnumerable<TModel> models, CancellationToken cancellationToken = default)
			=> AddAsync(models, outputMap: OutputMapper.MapOnce, cancellationToken: cancellationToken);

		public async Task<int> AddAsync(
			IEnumerable<TModel> models,
			Action<IEnumerable<TModel>, IEnumerable<TModel>, IEnumerable<PropertyInfo>, IEnumerable<PropertyInfo>> outputMap,
			CancellationToken cancellationToken = default
		)
		{
			return await Repository.ExecuteAsync(
				(conn) => AddAsync(conn, models, outputMap: outputMap, cancellationToken: cancellationToken)
			);
		}

		public virtual Task<int> AddAsync(
			IDbConnection connection,
			IEnumerable<TModel> models,
			Action<IEnumerable<TModel>, IEnumerable<TModel>, IEnumerable<PropertyInfo>, IEnumerable<PropertyInfo>> outputMap = null,
			IDbTransaction transaction = null,
			CancellationToken cancellationToken = default
		)
		{
			if (Repository is SqlRepository)
			{
				return connection.InsertBulkAsync(
					models,
					transaction: transaction,
					outputMap: outputMap,
					cancellationToken: cancellationToken
				);
			}

			return connection.ExecuteAsyncWithTransaction(
				async (conn, tran) => await conn.InsertAsync(models, transaction: tran),
				transaction: transaction
			);
		}

		#endregion

		#region Update

		#region ByModel

		public Task<int> UpdateAsync(TModel model, CancellationToken cancellationToken = default)
			=> UpdateAsync(new[] { model }, cancellationToken);

		public Task<int> UpdateAsync(IEnumerable<TModel> models, CancellationToken cancellationToken = default)
			=> UpdateAsync(models, outputMap: OutputMapper.MapOnce, cancellationToken: cancellationToken);

		public async Task<int> UpdateAsync(
			IEnumerable<TModel> models,
			Action<IEnumerable<TModel>, IEnumerable<TModel>, IEnumerable<PropertyInfo>, IEnumerable<PropertyInfo>> outputMap,
			CancellationToken cancellationToken = default
		)
		{
			return await Repository.ExecuteAsync(
				(conn) => UpdateAsync(conn, models, outputMap: outputMap, cancellationToken: cancellationToken)
			);
		}

		public virtual async Task<int> UpdateAsync(
			IDbConnection connection,
			IEnumerable<TModel> models,
			Action<IEnumerable<TModel>, IEnumerable<TModel>, IEnumerable<PropertyInfo>, IEnumerable<PropertyInfo>> outputMap = null,
			IDbTransaction transaction = null,
			CancellationToken cancellationToken = default
		)
		{
			if (Repository is SqlRepository)
			{
				return await connection.UpdateBulkAsync(
					models,
					transaction: transaction,
					outputMap: outputMap,
					cancellationToken: cancellationToken
				);
			}

			var success = await connection.ExecuteAsyncWithTransaction(
				async (conn, tran) => await conn.UpdateAsync(models, transaction: tran),
				transaction: transaction
			);

			if (success)
				return models.Count();
			else
				return 0;
		}

		#endregion

		public virtual Task<int> UpdateAsync(Action<TUpdate> updateSetup, Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null, CancellationToken cancellationToken = default)
			=> Repository.ExecuteAsync(
				(conn) => UpdateAsync(conn, updateSetup, filterSetup, optionsSetup, cancellationToken: cancellationToken)
			);

		public virtual Task<int> UpdateAsync(IDbConnection connection, Action<TUpdate> updateSetup, Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null, IDbTransaction transaction = null, CancellationToken cancellationToken = default)
		{
			var update = GetUpdate(updateSetup);
			var filter = GetFilter(filterSetup);
			var options = optionsSetup.Get();

			var builder = Source.AsQuery();
			UpdateQuery(builder, update, filter, options);

			var query = UpdateTemplate(builder, update, filter, options);
			return connection.ExecuteAsync(query, tran: transaction, cancellationToken: cancellationToken);
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

		protected virtual TUpdate GetUpdate(Action<TUpdate> updateSetup)
		{
			return updateSetup.Get(
				() => ChangeProxyFactory.Create<TUpdate>()
			);
		}

		protected virtual void Set(QueryBuilder<TModel> builder, TUpdate update)
		{
			if (update is UpdateOptions<TModel> updateOptions)
				updateOptions.Apply(builder);

			var changeTrack = update as IChangeTrack;
			if (changeTrack == null)
				return;

			foreach (var propertyName in changeTrack.Changed)
			{
				var property = builder.Table.Meta.Properties.FirstOrDefault(x => x.Name == propertyName);
				if (property is null)
					continue;

				var column = builder.Table.Column(property.Name);

				var updateProperty = TypeMeta.Get(update.GetType()).Properties.Single(x => x.Name == propertyName);
				var value = updateProperty.GetValue(update);

				if (value is string stringValue)
				{
					builder.Set(
						column,
						stringValue
					);
				}
				else if (value is DateTime dateTimeValue)
				{
					builder.Set(
						column,
						dateTimeValue
					);
				}
				else
				{
					builder.Set(
						column,
						value
					);
				}
			}
		}

		#endregion

		#region Upsert

		public Task<int> UpsertAsync(TModel model, CancellationToken cancellationToken = default)
			=> UpsertAsync(new[] { model }, cancellationToken);

		public Task<int> UpsertAsync(IEnumerable<TModel> models, CancellationToken cancellationToken = default)
			=> UpsertAsync(models, outputMap: OutputMapper.MapOnce, cancellationToken: cancellationToken);

		public async Task<int> UpsertAsync(
			IEnumerable<TModel> models,
			Action<IEnumerable<TModel>, IEnumerable<TModel>, IEnumerable<PropertyInfo>, IEnumerable<PropertyInfo>> outputMap,
			CancellationToken cancellationToken = default
		)
		{
			return await Repository.ExecuteAsync(
				(conn) => UpsertAsync(conn, models, outputMap: outputMap, cancellationToken: cancellationToken)
			);
		}

		public virtual Task<int> UpsertAsync(
			IDbConnection connection,
			IEnumerable<TModel> models,
			Action<IEnumerable<TModel>, IEnumerable<TModel>, IEnumerable<PropertyInfo>, IEnumerable<PropertyInfo>> outputMap = null,
			IDbTransaction transaction = null,
			CancellationToken cancellationToken = default
		)
		{
			if (!(Repository is SqlRepository))
				throw new NotSupportedException("Upsert is not supported on non-SQL repository connections.");

			return connection.UpsertBulkAsync(models, transaction: transaction, outputMap: outputMap, cancellationToken: cancellationToken);
		}

		#endregion

		#region Delete

		#region ByModel

		public async Task<bool> DeleteAsync(TModel model, CancellationToken cancellationToken = default)
		{
			return await Repository.ExecuteAsync((conn) => DeleteAsync(conn, model));
		}
		public virtual Task<bool> DeleteAsync(IDbConnection connection, TModel model, IDbTransaction transaction = null, CancellationToken cancellationToken = default)
			=> connection.DeleteAsync(model, transaction: transaction);

		public async Task<int> DeleteAsync(IEnumerable<TModel> models, CancellationToken cancellationToken = default)
		{
			return await Repository.ExecuteAsync(
				(conn) => DeleteAsync(conn, models, cancellationToken: cancellationToken)
			);
		}
		public virtual async Task<int> DeleteAsync(IDbConnection connection, IEnumerable<TModel> models, IDbTransaction transaction = null, CancellationToken cancellationToken = default)
		{
			if (Repository is SqlRepository)
			{
				return await connection.DeleteBulkAsync(
					models,
					transaction: transaction,
					cancellationToken: cancellationToken
				);
			}

			var sucess = await connection.ExecuteAsyncWithTransaction(
				(conn, tran) => conn.DeleteAsync(models, transaction: tran),
				transaction: transaction
			);
			return models.Count();
		}

		#endregion

		public Task<int> DeleteAsync(Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null, CancellationToken cancellationToken = default)
			=> Repository.ExecuteAsync((conn) => DeleteAsync(conn, filterSetup, optionsSetup, cancellationToken: cancellationToken));
		public virtual Task<int> DeleteAsync(
			IDbConnection connection,
			Action<TFilter> filterSetup = null,
			Action<TOptions> optionsSetup = null,
			IDbTransaction transaction = null,
			CancellationToken cancellationToken = default
		)
		{
			var filter = GetFilter(filterSetup);
			var options = optionsSetup.Get();
			var builder = Source.AsQuery();
			DeleteQuery(builder, filter, options);

			var query = DeleteTemplate(builder, options);
			return connection.ExecuteAsync(query, tran: transaction, cancellationToken: cancellationToken);
		}

		protected virtual void DeleteQuery(QueryBuilder<TModel> builder, TFilter filter, TOptions options)
			=> Query(builder, filter);

		protected virtual SqlBuilder.Template DeleteTemplate(QueryBuilder<TModel> builder, TOptions options)
		{
			return builder.AsDelete();
		}

		#endregion

		#region IAggregate

		#region Min

		public Task<TResult?> MinAsync<TResult>(string columnName, CancellationToken cancellationToken = default)
			where TResult : struct
			=> MinAsync<TResult>(Source.Column(columnName), null, cancellationToken);
		public Task<TResult?> MinAsync<TResult>(IDbConnection connection, string columnName, IDbTransaction transaction = null, CancellationToken cancellationToken = default)
			where TResult : struct
			=> MinAsync<TResult>(connection, Source.Column(columnName), null, transaction: transaction, cancellationToken: cancellationToken);

		public Task<TResult?> MinAsync<TResult>(string columnName, Action<TFilter> filterSetup = null, CancellationToken cancellationToken = default)
			where TResult : struct
			=> MinAsync<TResult>(Source.Column(columnName), filterSetup, cancellationToken);
		public Task<TResult?> MinAsync<TResult>(IDbConnection connection, string columnName, Action<TFilter> filterSetup = null, IDbTransaction transaction = null, CancellationToken cancellationToken = default)
			where TResult : struct
			=> MinAsync<TResult>(connection, Source.Column(columnName), filterSetup, transaction: transaction, cancellationToken: cancellationToken);

		public Task<TResult?> MinAsync<TResult>(Expression<Func<TModel, TResult>> columnExpression, Action<TFilter> filterSetup = null, CancellationToken cancellationToken = default)
			where TResult : struct
			=> MinAsync<TResult>(Source.Column(columnExpression), filterSetup, cancellationToken);
		public Task<TResult?> MinAsync<TResult>(IDbConnection connection, Expression<Func<TModel, TResult>> columnExpression, Action<TFilter> filterSetup = null, IDbTransaction transaction = null, CancellationToken cancellationToken = default)
			where TResult : struct
			=> MinAsync<TResult>(connection, Source.Column(columnExpression), filterSetup, transaction: transaction, cancellationToken: cancellationToken);

		protected virtual Task<TResult?> MinAsync<TResult>(Selector column, Action<TFilter> filterSetup = null, CancellationToken cancellationToken = default)
			where TResult : struct
			=> Repository.ExecuteAsyncWithTransaction((conn, tran) => MinAsync<TResult>(conn, column, filterSetup, tran, cancellationToken));

		public virtual Task<TResult?> MinAsync<TResult>(
			IDbConnection connection,
			Selector column,
			Action<TFilter> filterSetup = null,
			IDbTransaction transaction = null,
			CancellationToken cancellationToken = default
		)
			where TResult : struct
		{
			var builder = Source.AsQuery();
			var query = builder.AddTemplate($@"
				SELECT {column.Min()}
				FROM
					{Source}
					/**join**/
					/**innerjoin**/
					/**leftjoin**/
					/**rightjoin**/
				/**where**/
			");

			Filter(builder, GetFilter(filterSetup));
			return Repository.ExecuteAsync((conn) => conn.QueryFirstOrDefaultAsync<TResult?>(new CommandDefinition(query.RawSql, query.Parameters, cancellationToken: cancellationToken)));
		}

		#endregion

		#region Max

		public Task<TResult?> MaxAsync<TResult>(string columnName, CancellationToken cancellationToken = default)
			where TResult : struct
			=> MaxAsync<TResult>(Source.Column(columnName), null, cancellationToken);
		public Task<TResult?> MaxAsync<TResult>(IDbConnection connection, string columnName, IDbTransaction transaction = null, CancellationToken cancellationToken = default)
			where TResult : struct
			=> MaxAsync<TResult>(connection, Source.Column(columnName), null, transaction: transaction, cancellationToken: cancellationToken);

		public Task<TResult?> MaxAsync<TResult>(string columnName, Action<TFilter> filterSetup = null, CancellationToken cancellationToken = default)
			where TResult : struct
			=> MaxAsync<TResult>(Source.Column(columnName), filterSetup, cancellationToken);
		public Task<TResult?> MaxAsync<TResult>(IDbConnection connection, string columnName, Action<TFilter> filterSetup = null, IDbTransaction transaction = null, CancellationToken cancellationToken = default)
			where TResult : struct
			=> MaxAsync<TResult>(connection, Source.Column(columnName), filterSetup, transaction: transaction, cancellationToken: cancellationToken);

		public Task<TResult?> MaxAsync<TResult>(Expression<Func<TModel, TResult>> columnExpression, Action<TFilter> filterSetup = null, CancellationToken cancellationToken = default)
			where TResult : struct
			=> MaxAsync<TResult>(Source.Column(columnExpression), filterSetup, cancellationToken);
		public Task<TResult?> MaxAsync<TResult>(IDbConnection connection, Expression<Func<TModel, TResult>> columnExpression, Action<TFilter> filterSetup = null, IDbTransaction transaction = null, CancellationToken cancellationToken = default)
			where TResult : struct
			=> MaxAsync<TResult>(connection, Source.Column(columnExpression), filterSetup, transaction, cancellationToken);

		protected virtual Task<TResult?> MaxAsync<TResult>(Selector column, Action<TFilter> filterSetup = null, CancellationToken cancellationToken = default)
			where TResult : struct
			=> Repository.ExecuteAsync((conn) => MaxAsync<TResult>(conn, column, cancellationToken: cancellationToken));
		public virtual Task<TResult?> MaxAsync<TResult>(
			IDbConnection connection,
			Selector column,
			Action<TFilter> filterSetup = null,
			IDbTransaction transaction = null,
			CancellationToken cancellationToken = default
		)
			where TResult : struct
		{
			var builder = new QueryBuilder<TModel>();
			Filter(builder, GetFilter(filterSetup));

			var query = builder.AddTemplate($@"
					SELECT {column.Max()}
					FROM
						{Source}
						/**join**/
						/**innerjoin**/
						/**leftjoin**/
						/**rightjoin**/
					/**where**/
			"
			);
			return connection.QueryFirstOrDefaultAsync<TResult?>(new CommandDefinition(query.RawSql, query.Parameters, transaction: transaction, cancellationToken: cancellationToken));
		}

		#endregion

		#endregion

		#region Replace

		public Task<(int Deleted, int Added)> ReplaceAsync(
			Action<TFilter> filterSetup,
			IEnumerable<TModel> models,
			CancellationToken cancellationToken = default
		)
			=> Repository.ExecuteAsyncWithTransaction((conn, tran) => ReplaceAsync(conn, tran, filterSetup, models, cancellationToken));

		public virtual Task<(int Deleted, int Added)> ReplaceAsync(
			IDbConnection conn,
			IDbTransaction tran,
			Action<TFilter> filterSetup,
			IEnumerable<TModel> models,
			CancellationToken cancellationToken = default
		)
		{
			return conn.ExecuteAsyncWithTransaction(
				async (conn, tran) =>
				{
					var deleted = await DeleteAsync(
						conn,
						filterSetup,
						transaction: tran,
						cancellationToken: cancellationToken
					);

					if (models == null)
						return (deleted, 0);

					var added = await AddAsync(
						conn,
						models,
						outputMap: OutputMapper.Map,
						transaction: tran,
						cancellationToken: cancellationToken
					);

					return (deleted, added);
				},
				transaction: tran
			);
		}

		#endregion

		protected TFilter GetFilter(Action<TFilter> filterSetup)
		{
			return filterSetup.Get(
				() => ChangeProxyFactory.Create<TFilter>()
			);
		}

		protected virtual void Filter(QueryBuilder<TModel> builder, TFilter filter)
		{
			foreach (var propertyName in GetFilterConditions(filter))
			{
				var property = builder.Table.Meta.Properties.SingleOrDefault(x => x.Name == propertyName);
				if (property is null)
					continue;

				var column = builder.Table.Column(property.Name);

				var filterProperty = TypeMeta.Get(filter.GetType()).Properties.Single(x => x.Name == propertyName);
				var value = filterProperty.GetValue(filter);

				if (value is null)
				{
					builder.WhereNull(column);
				}
				else if (value is string stringValue)
				{
					builder.Where(
						column,
						stringValue
					);
				}
				else if (value is DateTime dateTimeValue)
				{
					builder.Where(
						column,
						dateTimeValue
					);
				}
				else
				{
					builder.Where(
						column,
						value
					);
				}
			}
		}

		protected virtual IEnumerable<string> GetFilterConditions(TFilter filter)
			=> (filter as IChangeTrack)?.Changed ?? Array.Empty<string>();

		protected virtual void Join(QueryBuilder<TModel> builder, TFilter filter) { }

		protected void IfChanged<T, R>(T filter, Expression<Func<T, R>> expression, Action action)
		{
			if (filter is IChangeTrack changeTrack)
			{
				var name = ExpressionHelper.GetMemberName(expression);
				if (changeTrack.Changed.Contains(name))
					action();
			}
		}

		Task<R> IEntity<TModel, TUpdate, TFilter, TOptions>.ExecuteAsyncWithTransaction<R>(Func<IDbConnection, IDbTransaction, Task<R>> function)
			=> Repository.ExecuteAsyncWithTransaction(function);
	}

	public class Entity<TModel, TFilter, TOptions>
		: Entity<TModel, TModel, TFilter, TOptions>, IEntity<TModel, TFilter, TOptions>
		where TModel : class
		where TFilter : class
		where TOptions : DbQueryOptions, new()
	{
		public Entity(DbRepository repository, Table<TModel> source)
			: base(repository, source)
		{
		}
	}

	public class Entity<T, TOptions>
		: Entity<T, T, TOptions>, IEntity<T, TOptions>
		where T : class
		where TOptions : DbQueryOptions, new()
	{
		public Entity(DbRepository repository, Table<T> source)
			: base(repository, source)
		{
		}
	}

	public class Entity<T>
		: Entity<T, DbQueryOptions>, IEntity<T>
		where T : class
	{
		public Entity(DbRepository repository, Table<T> source)
			: base(repository, source)
		{
		}
	}
}
