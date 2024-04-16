using Simpleverse.Repository.Db.Operations;
using System;
using System.Data;
using System.Threading.Tasks;

namespace Simpleverse.Repository.Db.Entity
{
	public interface IEntity<TModel, TUpdate, TFilter, TOptions>
		: Repository.Entity.IEntity<TModel, TUpdate, TFilter, TOptions>,
			IAddDb<TModel>,
			IAggregateDb<TFilter>,
			IDeleteDb<TModel>,
			IDeleteDb<TModel, TFilter, TOptions>,
			IQueryDb<TModel, TFilter, TOptions>,
			IReplaceDb<TModel, TFilter>,
			IUpdateDb<TModel>,
			IUpdateDb<TUpdate, TFilter, TOptions>,
			IUpsertDb<TModel>
		where TModel : class
		where TUpdate : class
		where TFilter : class
		where TOptions : class, new()
	{
		internal Task<R> ExecuteAsyncWithTransaction<R>(Func<IDbConnection, IDbTransaction, Task<R>> function);
	}

	public interface IEntity<TModel, TFilter, TOptions>
		: IEntity<TModel, TModel, TFilter, TOptions>, Repository.Entity.IEntity<TModel, TFilter, TOptions>
		where TModel : class
		where TFilter : class
		where TOptions : class, new()
	{
	}

	public interface IEntity<TModel, TOptions>
		: IEntity<TModel, TModel, TOptions>, Repository.Entity.IEntity<TModel, TOptions>
		where TModel : class
		where TOptions : class, new()
	{
	}

	public interface IEntity<TModel>
		: IEntity<TModel, DbQueryOptions>
		where TModel : class
	{

	}
}
