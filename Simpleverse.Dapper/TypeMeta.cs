using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

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

		public TypeMeta(Type type)
		{
			TableName = SqlMapperWrapper.GetTableName(type);
			Properties = SqlMapperWrapper.TypePropertiesCache(type);
			PropertiesKey = SqlMapperWrapper.KeyPropertiesCache(type);
			PropertiesComputed = SqlMapperWrapper.ComputedPropertiesCache(type);
			PropertiesExplicit = SqlMapperWrapper.ExplicitKeyPropertiesCache(type);
			PropertiesExceptKeyAndComputed = Properties.Except(PropertiesKey.Union(PropertiesComputed)).ToList();
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
		public static string ColumnList(this IEnumerable<PropertyInfo> properties, string prefix = null, string suffix = null)
		{
			if (!string.IsNullOrEmpty(prefix))
				prefix = prefix + ".";

			return string.Join(", ", properties.Select(x => prefix + x.Name + suffix));
		}

		public static string ColumnListEquals(this IEnumerable<PropertyInfo> properties, string separator, string leftPrefix = "Target", string rightPrefix = "Source")
		{
			if (!string.IsNullOrEmpty(leftPrefix))
				leftPrefix = leftPrefix + ".";

			if (!string.IsNullOrEmpty(rightPrefix))
				rightPrefix = rightPrefix + ".";

			return string.Join(separator, properties.Select(x => $"{leftPrefix}{x.Name} = {rightPrefix}{x.Name}"));
		}

		public static string ParameterList(this IEnumerable<PropertyInfo> properties, string suffix = null)
		{
			return string.Join(", ", properties.Select(x => x.ParameterName(suffix: suffix)));
		}

		public static string ParameterName(this PropertyInfo property, string suffix = null)
		{
			return $"@{property.Name}{suffix}";
		}

		public static string ColumnListDifferenceCheck(this IEnumerable<PropertyInfo> properties, string separator = " OR ", string leftPrefix = "Target", string rightPrefix = "Source")
		{
			if (!string.IsNullOrEmpty(leftPrefix))
				leftPrefix = leftPrefix + ".";

			if (!string.IsNullOrEmpty(rightPrefix))
				rightPrefix = rightPrefix + ".";

			return string.Join(
				separator,
				properties.Select(x =>
					$"NULLIF({leftPrefix}{x.Name}, {rightPrefix}{x.Name}) IS NOT NULL OR NULLIF({rightPrefix}{x.Name}, {leftPrefix}{x.Name}) IS NOT NULL"
				)
			);
		}
	}
}
