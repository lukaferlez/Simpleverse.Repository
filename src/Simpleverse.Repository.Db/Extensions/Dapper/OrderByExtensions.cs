using Dapper;
using Simpleverse.Repository.Entity;

namespace Simpleverse.Repository.Db.Extensions.Dapper
{
	public static class OrderByExtensions
	{
		public static void OrderBy(this SqlBuilder sqlBuilder, OrderBy orderBy)
		{
			foreach (var clause in orderBy.Clauses)
			{
				sqlBuilder.OrderBy((Selector)clause.selector, clause.direction);
			}
		}

		public static SqlBuilder OrderBy(
			this SqlBuilder sqlBuilder,
			string name,
			OrderDirection orderDirection = OrderDirection.Ascending,
			string alias = null
		)
			=> sqlBuilder.OrderBy(new Selector(name, alias), orderDirection);

		public static SqlBuilder OrderBy(
			this SqlBuilder builder,
			Selector selector,
			OrderDirection orderDirection = OrderDirection.Ascending
		)
		{
			builder.OrderBy(selector.Order(orderDirection).ToString());
			return builder;
		}
	}
}