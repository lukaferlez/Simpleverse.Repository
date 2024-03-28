using Simpleverse.Repository.Operations;
using System;
using System.Data;
using System.Threading.Tasks;

namespace Simpleverse.Repository.Db.Operations
{
	public interface IQueryGetDb<TModel, TFilter, TOptions> : IQueryGet<TModel, TFilter, TOptions>
		where TModel : class
		where TFilter : IQueryFilter, new()
		where TOptions : QueryOptions, new()
	{
		Task<TModel> GetAsync(
			IDbConnection connection,
			Action<TFilter> filterSetup = null,
			Action<TOptions> optionsSetup = null,
			IDbTransaction transaction = null
		);

		Task<T> GetAsync<T>(
			IDbConnection connection,
			Action<TFilter> filterSetup = null,
			Action<TOptions> optionsSetup = null,
			IDbTransaction transaction = null
		);
	}
}
