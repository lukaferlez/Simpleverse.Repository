using Simpleverse.Repository.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Simpleverse.Repository.Db
{
	public class Selector : ISelector
	{
		public string Column { get; protected set; }

		public string TableAlias { get; protected set; }

		public string Alias { get; protected set; }

		public string Source { get; }

		public List<Func<string, string>> Selectors { get; }

		public Selector(string source)
		{
			Source = source;
			Selectors = new List<Func<string, string>>();
		}

		public Selector(string name, string alias)
			: this(DbRepository.ColumnReference(name, alias))
		{
			if (string.IsNullOrWhiteSpace(name))
				throw new ArgumentNullException(nameof(name));

			Column = name;
			TableAlias = alias;
			Alias = name;
		}

		public Selector Count()
		{
			Selectors.Add(previous => $"COUNT({previous})");
			return this;
		}

		public Selector Avg()
		{
			Selectors.Add((previous) => $"AVERAGE({previous})");
			return this;
		}

		public Selector Max()
		{
			Selectors.Add((previous) => $"MAX({previous})");
			return this;
		}

		public Selector Min()
		{
			Selectors.Add((previous) => $"MIN({previous})");
			return this;
		}

		public Selector Sum()
		{
			Selectors.Add((previous) => $"SUM({previous})");
			return this;
		}

		public Selector Lower()
		{
			Selectors.Add((previous) => $"LOWER({previous})");
			return this;
		}

		public Selector Upper()
		{
			Selectors.Add((previous) => $"UPPER({previous})");
			return this;
		}

		public Selector IsNull(object defaultValue)
		{
			Selectors.Add((previous) => $"ISNULL({previous}, {defaultValue})");
			return this;
		}

		public Selector IsNull()
		{
			Selectors.Add((previous) => $"{previous} IS NULL");
			return this;
		}

		public Selector IsNotNull()
		{
			Selectors.Add((previous) => $"{previous} IS NOT NULL");
			return this;
		}

		public Selector In(IEnumerable<string> values, bool not = false)
		{
			if (values == null || !values.Any())
				return this;
			return In(string.Join(',', values.Select(x => $"'{x.Replace("'", "''")}'")), not);
		}

		public Selector In<T>(IEnumerable<T> values, bool not = false)
		{
			if (values == null || !values.Any())
				return this;
			var valuesJoined = values.Join(',');
			var tType = typeof(T);
			if (tType.IsEnum)
				valuesJoined = string.Join(',', values.Select(x => Enum.Format(tType, x, "d")));

			return In(valuesJoined, not);
		}

		public Selector In(IEnumerable<Guid> values, bool not = false)
			=> In(values.Select(x => x.ToString()), not);

		public Selector In(IEnumerable<DateTime> values, bool not = false)
		{
			if (values == null || !values.Any())
				return this;

			return In(string.Join(',', values.Select(x => $"'{x:yyyy-MM-ddThh:mm:ss.fff}'")), not);
		}

		private Selector In(string valuesJoined, bool not = false)
		{
			return Is($"({valuesJoined})", not ? "NOT IN" : "IN");
		}

		public Selector Is<T>(T value, string condition = "=")
		{
			Selectors.Add((previous) => $"{previous} {condition} {value}");
			return this;
		}

		public Selector Is(Selector selector, string condition = "=")
		{
			Selectors.Add((previous) => $"{previous} {condition} {selector}");
			return this;
		}

		public Selector StartWith(string text, bool fromParameter = false)
		{
			return Like(
				text,
				wildcardEnd: true,
				fromParameter: fromParameter
			);
		}

		public Selector EndsWith(string text, bool fromParameter = false)
		{
			return Like(
				text,
				wildcardStart: true,
				fromParameter: fromParameter
			);
		}

		public Selector Contains(string text, bool fromParameter = false)
		{
			return Like(
				text,
				wildcardStart: true,
				wildcardEnd: true,
				fromParameter: fromParameter
			);
		}

		public Selector Like(string text, bool wildcardStart = false, bool wildcardEnd = false, bool fromParameter = false)
		{
			var likeValue = text;
			if (fromParameter)
			{
				if (wildcardStart)
					likeValue = "'%', " + likeValue;

				if (wildcardEnd)
					likeValue = likeValue + ", '%'";

				if (wildcardEnd || wildcardStart)
					likeValue = $"CONCAT({likeValue})";
			}
			else
			{
				if (wildcardStart)
					likeValue = "%" + likeValue;

				if (wildcardEnd)
					likeValue = likeValue + "%";
			}

			return Is(likeValue, "LIKE");
		}

		public Selector StringAgg(string separator)
		{
			Selectors.Add((previous) => $"STRING_AGG({previous}, '{separator}')");
			return this;
		}

		public Selector AsBit(bool inverse = false)
		{
			if (inverse)
				Selectors.Add(previous => $"CASE WHEN {previous} THEN 0 ELSE 1 END");
			else
				Selectors.Add(previous => $"CASE WHEN {previous} THEN 1 ELSE 0 END");

			return this;
		}

		public Selector Day()
		{
			Selectors.Add(previous => $"DAY{previous}");
			return this;
		}

		public Selector Month()
		{
			Selectors.Add(previous => $"MONTH({previous})");
			return this;
		}

		public Selector Year()
		{
			Selectors.Add(previous => $"YEAR({previous})");
			return this;
		}

		public Selector Order(OrderDirection orderDirection = OrderDirection.Ascending)
		{
			if (orderDirection == OrderDirection.Descending)
				Selectors.Add((previous) => $"{previous} DESC");

			return this;
		}

		public virtual Selector As(string alias)
		{
			Alias = alias;
			return this;
		}

		#region ToString

		public override string ToString()
			=> ToString(false);

		public string ToString(bool withAlias)
		{
			var result = Source;
			foreach (var selector in Selectors)
			{
				result = selector(result);
			}

			if (string.IsNullOrWhiteSpace(Alias) || !withAlias)
				return result;

			return result + $" AS {Alias}";
		}

		#endregion
	}
}
