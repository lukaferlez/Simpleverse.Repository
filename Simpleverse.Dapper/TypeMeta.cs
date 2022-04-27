using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Dapper;
using System.Text;

namespace Simpleverse.Dapper
{
	public class TypeMeta
	{
		private static ConcurrentDictionary<Type, TypeMeta> _meta = new ConcurrentDictionary<Type, TypeMeta>();

		public string TableName { get; }
		public IList<PropertyInfo> Properties { get; }
		public IList<PropertyInfo> PropertiesKey { get; }
		public IList<PropertyInfo> PropertiesComputed { get; }
		public IList<PropertyInfo> PropertiesExplicit { get; }
		public IList<PropertyInfo> PropertiesExceptKeyAndComputed { get; }
		public IList<PropertyInfo> PropertiesExceptKeyComputedAndExplicit { get; }
		public IList<PropertyInfo> PropertiesKeyAndExplicit { get; }
		public IList<PropertyInfo> PropertiesExceptComputed { get; }

		public TypeMeta(Type type)
		{
			TableName = SqlMapperWrapper.GetTableName(type);
			Properties = SqlMapperWrapper.TypePropertiesCache(type);
			PropertiesKey = SqlMapperWrapper.KeyPropertiesCache(type);
			PropertiesComputed = SqlMapperWrapper.ComputedPropertiesCache(type);
			PropertiesExplicit = SqlMapperWrapper.ExplicitKeyPropertiesCache(type);
			PropertiesExceptComputed = Properties.Except(PropertiesComputed).ToList();
			PropertiesExceptKeyAndComputed = PropertiesExceptComputed.Except(PropertiesKey).ToList();
			PropertiesExceptKeyComputedAndExplicit = PropertiesExceptKeyAndComputed.Except(PropertiesExplicit).ToList();
			PropertiesKeyAndExplicit = PropertiesExplicit.Union(PropertiesKey).ToList();
		}

		public static TypeMeta Get(Type type)
		{
			return _meta.GetOrAdd(type, new TypeMeta(type));
		}

		public static TypeMeta Get<T>()
		{
			return Get(typeof(T));
		}
	}

	public static class PropertyEnumerableExtensions
	{
		public static string ColumnList(this IEnumerable<string> columnNames, string prefix = null, string suffix = null)
		{
			if (!string.IsNullOrEmpty(prefix))
				prefix = prefix + ".";

			return string.Join(", ", columnNames.Select(x => prefix + x + suffix));
		}

		public static string ColumnList(this IEnumerable<PropertyInfo> properties, string prefix = null, string suffix = null)
		{
			return ColumnList(properties.Select(x => x.Name), prefix: prefix, suffix: suffix);
		}

		public static string ColumnListEquals(this IEnumerable<string> columNames, string separator, string leftPrefix = "Target", string rightPrefix = "Source")
		{
			if (!string.IsNullOrEmpty(leftPrefix))
				leftPrefix = leftPrefix + ".";

			if (!string.IsNullOrEmpty(rightPrefix))
				rightPrefix = rightPrefix + ".";

			return string.Join(separator, columNames.Select(x => $"{leftPrefix}{x} = {rightPrefix}{x}"));
		}

		public static string ColumnListEquals(this IEnumerable<PropertyInfo> properties, string separator, string leftPrefix = "Target", string rightPrefix = "Source")
		{
			return ColumnListEquals(properties.Select(x => x.Name), separator, leftPrefix: leftPrefix, rightPrefix: rightPrefix);
		}

		public static string ParameterList(this IEnumerable<string> columNames, string suffix = null)
		{
			return string.Join(", ", columNames.Select(x => x.ParameterName(suffix: suffix)));
		}

		public static string ParameterList(this IEnumerable<PropertyInfo> properties, string suffix = null)
		{
			return ParameterList(properties.Select(x => x.Name), suffix: suffix);
		}

		public static string ParameterName(this string columnName, string suffix = null)
		{
			return $"@{columnName}{suffix}";
		}

		public static string ParameterName(this PropertyInfo property, string suffix = null)
		{
			return ParameterName(property.Name, suffix: suffix);
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
					$"NULLIF({leftPrefix}{x}, {rightPrefix}{x}) IS NOT NULL OR NULLIF({rightPrefix}{x}, {leftPrefix}{x}) IS NOT NULL"
				)
			);
		}

		public static string ColumnListDifferenceCheck(this IEnumerable<PropertyInfo> properties, string separator = " OR ", string leftPrefix = "Target", string rightPrefix = "Source")
		{
			return ColumnListDifferenceCheck(properties.Select(x => x.Name), separator: separator, leftPrefix: leftPrefix, rightPrefix: rightPrefix);
		}

		public static (string query, DynamicParameters parameters) ColumnListAsValueParamaters<T>(this IEnumerable<PropertyInfo> properties, IEnumerable<T> entities)
		{
			var parameters = new DynamicParameters();
			var sb = new StringBuilder("VALUES");
			sb.AppendLine();

			int index = 0;
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
