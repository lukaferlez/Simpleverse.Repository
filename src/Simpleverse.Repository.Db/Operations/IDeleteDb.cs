using Simpleverse.Repository.Operations;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Simpleverse.Repository.Db.Operations
{
	public interface IDeleteDb<T> : IDelete<T>
		where T : class
	{
		Task<bool> DeleteAsync(IDbConnection connection, T model, IDbTransaction transaction = null);
		Task<int> DeleteAsync(IDbConnection connection, IEnumerable<T> models, IDbTransaction transaction = null);
	}

	public interface IDeleteDb<TModel, TFilter, TOptions> : IDeleteDb<TModel>, IDelete<TModel, TFilter, TOptions>
		where TModel : class
		where TFilter : class
	{
		Task<int> DeleteAsync(IDbConnection connection, Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null, IDbTransaction transaction = null);
	}
}