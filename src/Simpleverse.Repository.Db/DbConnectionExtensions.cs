using System;
using System.Data.Common;
using System.Threading.Tasks;

namespace Simpleverse.Repository.Db
{
	public static class DbConnectionExtensions
	{
		public static async Task<R> ExecuteAsyncWithTransaction<R>(this DbConnection conn, Func<DbConnection, DbTransaction, Task<R>> function)
		{
			using (var tran = conn.BeginTransaction())
			{
				var result = await function(conn, tran);
				tran.Commit();
				return result;
			}
		}
	}
}
