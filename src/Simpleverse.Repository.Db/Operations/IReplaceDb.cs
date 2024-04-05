using Simpleverse.Repository.Operations;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Simpleverse.Repository.Db.Operations
{
	public interface IReplaceDb<TModel, TFilter> : IReplace<TModel, TFilter>
		where TModel : class
		where TFilter : class
	{
		Task<(int Deleted, int Added)> ReplaceAsync(
			IDbConnection conn,
			IDbTransaction tran,
			Action<TFilter> filterSetup,
			IEnumerable<TModel> models
		);
	}
}
