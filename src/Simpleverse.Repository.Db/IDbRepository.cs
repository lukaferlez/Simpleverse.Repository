using Dapper;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Simpleverse.Repository.Db
{
	public interface IDbRepository
	{
		Task<int> ExecuteAsync(SqlBuilder.Template query);
		Task<IEnumerable<dynamic>> QueryAsync(string rawSql, object parameters);
		Task<IEnumerable<R>> QueryAsync<R>(string rawSql, object parameters);
	}
}