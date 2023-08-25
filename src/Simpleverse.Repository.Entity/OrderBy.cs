using System.Collections.Generic;

namespace Simpleverse.Repository.Entity
{
	public class OrderBy
	{
		public List<(ISelector selector, OrderDirection direction)> Clauses { get; set; } = new List<(ISelector selector, OrderDirection direction)>();

		public OrderBy By(ISelector selector)
		{
			Clauses.Add((selector, OrderDirection.Ascending));
			return this;
		}
		public OrderBy ByDescending(ISelector selector)
		{
			Clauses.Add((selector, OrderDirection.Descending));
			return this;
		}
	}

	public enum OrderDirection
	{
		Descending = 0,
		Ascending = 1
	}
}
