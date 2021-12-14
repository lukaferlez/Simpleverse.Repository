using System.Data;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;

namespace Simpleverse.Dapper.SqlServer
{
	public static class FunctionExtensions
	{
		public static int Truncate<T>(
			this SqlConnection connection
		)
		{
			return connection.TruncateAsync<T>().Result;
		}

		public static async Task<int> TruncateAsync<T>(
			this SqlConnection connection
		)
		{
			var typeMeta = TypeMeta.Get<T>();

			var wasClosed = connection.State == ConnectionState.Closed;
			if (wasClosed) connection.Open();

			var result = await connection.ExecuteAsync($"TRUNCATE TABLE {typeMeta.TableName}");

			if (wasClosed) connection.Close();

			return result;
		}
	}
}
