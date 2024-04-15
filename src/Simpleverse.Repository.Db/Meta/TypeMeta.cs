using Simpleverse.Repository.Db.Entity;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Simpleverse.Repository.Db.Meta
{
	public class TypeMeta
	{
		private static ConcurrentDictionary<Type, TypeMeta> _meta = new ConcurrentDictionary<Type, TypeMeta>();

		public string TableName { get; }
		public IList<PropertyInfo> Properties { get; }
		public IList<PropertyInfo> PropertiesKey { get; }
		public IList<PropertyInfo> PropertiesComputed { get; }
		public IList<PropertyInfo> PropertiesKeyAndComputed { get; }
		public IList<PropertyInfo> PropertiesExplicit { get; }
		public IList<PropertyInfo> PropertiesExceptKeyAndComputed { get; }
		public IList<PropertyInfo> PropertiesExceptKeyComputedAndExplicit { get; }
		public IList<PropertyInfo> PropertiesKeyAndExplicit { get; }
		public IList<PropertyInfo> PropertiesExceptComputed { get; }
		public bool IsProjection { get; }

		public TypeMeta(Type type)
		{
			TableName = SqlMapperWrapper.GetTableName(type);
			Properties = SqlMapperWrapper.TypePropertiesCache(type);
			PropertiesKey = SqlMapperWrapper.KeyPropertiesCache(type);
			PropertiesComputed = SqlMapperWrapper.ComputedPropertiesCache(type);
			PropertiesKeyAndComputed = PropertiesKey.Union(PropertiesComputed).ToList();
			PropertiesExplicit = SqlMapperWrapper.ExplicitKeyPropertiesCache(type);
			PropertiesExceptComputed = Properties.Except(PropertiesComputed).ToList();
			PropertiesExceptKeyAndComputed = Properties.Except(PropertiesKeyAndComputed).ToList();
			PropertiesExceptKeyComputedAndExplicit = PropertiesExceptKeyAndComputed.Except(PropertiesExplicit).ToList();
			PropertiesKeyAndExplicit = PropertiesExplicit.Union(PropertiesKey).ToList();
			IsProjection = Array.Exists(type.GetInterfaces(), i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IProject<>));
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
}
