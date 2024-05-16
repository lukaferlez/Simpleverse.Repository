using Dapper;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Simpleverse.Repository.Db
{
	public static class PropertyEnumerableExtensions
	{
		public static string ColumnList(this IEnumerable<string> columnNames, string prefix = null, string suffix = null)
		{
			if (!string.IsNullOrEmpty(prefix))
				prefix = prefix + ".";

			return string.Join(", ", columnNames.Select(x => prefix + $"[{x + suffix}]"));
		}

		public static string ColumnList(this IEnumerable<PropertyInfo> properties, string prefix = null, string suffix = null)
		{
			return properties.Select(x => x.Name).ColumnList(prefix: prefix, suffix: suffix);
		}

		public static string ColumnListEquals(this IEnumerable<string> columNames, string separator, string leftPrefix = "Target", string rightPrefix = "Source")
		{
			if (!string.IsNullOrEmpty(leftPrefix))
				leftPrefix = leftPrefix + ".";

			if (!string.IsNullOrEmpty(rightPrefix))
				rightPrefix = rightPrefix + ".";

			return string.Join(separator, columNames.Select(x => $"{leftPrefix}[{x}] = {rightPrefix}[{x}]"));
		}

		public static string ColumnListEquals(this IEnumerable<PropertyInfo> properties, string separator, string leftPrefix = "Target", string rightPrefix = "Source")
		{
			return properties.Select(x => x.Name).ColumnListEquals(separator, leftPrefix: leftPrefix, rightPrefix: rightPrefix);
		}

		public static string ParameterList(this IEnumerable<string> columNames, string suffix = null)
		{
			return string.Join(", ", columNames.Select(x => x.ParameterName(suffix: suffix)));
		}

		public static string ParameterList(this IEnumerable<PropertyInfo> properties, string suffix = null)
		{
			return properties.Select(x => x.Name).ParameterList(suffix: suffix);
		}

		public static string ParameterName(this string columnName, string suffix = null)
		{
			return $"@{columnName}{suffix}";
		}

		public static string ParameterName(this PropertyInfo property, string suffix = null)
		{
			return property.Name.ParameterName(suffix: suffix);
		}

		public static string ColumnListDifferenceCheck(this IEnumerable<string> columNames, string separator = " OR ", string leftPrefix = "Target", string rightPrefix = "Source")
		{
			if (!string.IsNullOrEmpty(leftPrefix))
				leftPrefix = leftPrefix + ".";

			if (!string.IsNullOrEmpty(rightPrefix))
				rightPrefix = rightPrefix + ".";

			return string.Join(
				separator,
				columNames.Select(x =>
					$"NULLIF({leftPrefix}[{x}], {rightPrefix}[{x}]) IS NOT NULL OR NULLIF({rightPrefix}[{x}], {leftPrefix}[{x}]) IS NOT NULL"
				)
			);
		}

		public static string ColumnListDifferenceCheck(this IEnumerable<PropertyInfo> properties, string separator = " OR ", string leftPrefix = "Target", string rightPrefix = "Source")
		{
			return properties.Select(x => x.Name).ColumnListDifferenceCheck(separator: separator, leftPrefix: leftPrefix, rightPrefix: rightPrefix);
		}

		public static (string query, DynamicParameters parameters) ColumnListAsValueParamaters<T>(this IEnumerable<PropertyInfo> properties, IEnumerable<T> entities)
		{
			var parameters = new DynamicParameters();
			var sb = new StringBuilder("VALUES");
			sb.AppendLine();

			var index = 0;
			foreach (var entity in entities)
			{
				var parameterList = properties.ParameterList($"_{index}");
				if (index > 0)
					sb.AppendLine(",");
				sb.Append($"({parameterList})");

				foreach (var column in properties)
				{
					parameters.Add(column.ParameterName($"_{index}"), column.GetValue(entity));
				}

				index++;
			}

			return (sb.ToString(), parameters);
		}
	}
}
