using Simpleverse.Repository.Operations;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Simpleverse.Repository.Db.Operations
{
	public interface IQueryListDb<TModel, TFilter, TOptions> : IQueryList<TModel, TFilter, TOptions>
		where TModel : class
		where TFilter : class
		where TOptions : class, new()
	{
		Task<IEnumerable<TModel>> ListAsync(
			IDbConnection connection,
			Action<TFilter> filterSetup = null,
			Action<TOptions> optionsSetup = null,
			IDbTransaction transaction = null
		);

		Task<IEnumerable<T>> ListAsync<T>(
			IDbConnection connection,
			Action<TFilter> filterSetup = null,
			Action<TOptions> optionsSetup = null,
			IDbTransaction transaction = null
		);

		Task<IEnumerable<TModel>> ListAsync(
			IDbConnection connection,
			TFilter filter,
			TOptions options,
			IDbTransaction transaction = null
		);

		Task<IEnumerable<T>> ListAsync<T>(
			IDbConnection connection,
			TFilter filter,
			TOptions options,
			IDbTransaction transaction = null
		);
	}
}
