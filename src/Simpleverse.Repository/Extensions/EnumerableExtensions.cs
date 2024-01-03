using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Simpleverse.Repository.Extensions
{
	public static class EnumerableExtensions
	{
		public static string Join<T>(this IEnumerable<T> values, char separator, CultureInfo culture = null)
		{
			culture ??= CultureInfo.InvariantCulture;
			var formatedValues = values.Select(x => string.Format(culture, "{0}", x));
			return string.Join(separator, formatedValues);
		}
	}
}
