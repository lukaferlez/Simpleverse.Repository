using System.Data;
using System.Threading.Tasks;
using Dapper;
using Simpleverse.Repository.Db.Meta;

namespace Simpleverse.Repository.Db.Extensions
{
	public static class TruncateExtensions
	{
		public static int Truncate<T>(
			this IDbConnection connection
		)
		{
			return connection.TruncateAsync<T>().Result;
		}

		public static async Task<int> TruncateAsync<T>(
			this IDbConnection connection
		)
		{
			var typeMeta = TypeMeta.Get<T>();
			return await connection.TruncateAsync(typeMeta.TableName);
		}

		public static async Task<int> TruncateAsync(
			this IDbConnection connection,
			string tableName
		)
		{
			var wasClosed = connection.State == ConnectionState.Closed;
			if (wasClosed) connection.Open();

			var result = await connection.ExecuteAsync($"TRUNCATE TABLE {tableName};");

			if (wasClosed) connection.Close();

			return result;
		}
	}
}
