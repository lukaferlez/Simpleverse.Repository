using Dapper;

namespace Simpleverse.Repository.Db.Extensions.Dapper
{
	public static class GroupExtensions
	{
		public static SqlBuilder GroupBy(this SqlBuilder sqlBuilder, Selector selector)
		{
			return sqlBuilder.GroupBy(selector.ToString());
		}
	}
}
