using Dapper;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Simpleverse.Repository.Db
{
	public interface IDbRepository
	{
		Task<int> ExecuteAsync(SqlBuilder.Template query, CancellationToken cancellationToken = default);
		Task<IEnumerable<dynamic>> QueryAsync(string rawSql, object parameters, CancellationToken cancellationToken = default);
		Task<IEnumerable<R>> QueryAsync<R>(string rawSql, object parameters, CancellationToken cancellationToken = default);
	}
}