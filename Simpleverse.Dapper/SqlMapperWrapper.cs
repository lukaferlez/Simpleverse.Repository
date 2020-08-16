using System;
using System.Reflection;
using System.Collections.Generic;
using Dapper.Contrib.Extensions;

namespace Simpleverse.Dapper
{
	public static class SqlMapperWrapper
	{
		private static MethodInfo GetTableNameMethod { get; set; }
		private static MethodInfo TypePropertiesCacheMethod { get; set; }
		private static MethodInfo KeyPropertiesCacheMethod { get; set; }
		private static MethodInfo ExplicitKeyPropertiesCacheMethod { get; set; }
		private static MethodInfo ComputedPropertiesCacheMethod { get; set; }

		public static string GetTableName(Type type)
		{
			if (GetTableNameMethod == null)
			{
				var sqlMapperType = typeof(SqlMapperExtensions);

				GetTableNameMethod = sqlMapperType.GetMethod("GetTableName", BindingFlags.Static | BindingFlags.NonPublic, Type.DefaultBinder, new[] { typeof(Type) }, null);
			}

			return (string) GetTableNameMethod.Invoke(null, new[] { type });
		}

		public static List<PropertyInfo> TypePropertiesCache(Type type)
		{
			if (TypePropertiesCacheMethod == null)
			{
				var sqlMapperType = typeof(SqlMapperExtensions);

				TypePropertiesCacheMethod = sqlMapperType.GetMethod("TypePropertiesCache", BindingFlags.Static | BindingFlags.NonPublic, Type.DefaultBinder, new[] { typeof(Type) }, null);
			}

			return (List<PropertyInfo>) TypePropertiesCacheMethod.Invoke(null, new[] { type });
		}
		
		public static List<PropertyInfo> KeyPropertiesCache(Type type)
		{
			if (KeyPropertiesCacheMethod == null)
			{
				var sqlMapperType = typeof(SqlMapperExtensions);

				KeyPropertiesCacheMethod = sqlMapperType.GetMethod("KeyPropertiesCache", BindingFlags.Static | BindingFlags.NonPublic, Type.DefaultBinder, new[] { typeof(Type) }, null);
			}

			return (List<PropertyInfo>)KeyPropertiesCacheMethod.Invoke(null, new[] { type });
		}

		public static List<PropertyInfo> ExplicitKeyPropertiesCache(Type type)
		{
			if (ExplicitKeyPropertiesCacheMethod == null)
			{
				var sqlMapperType = typeof(SqlMapperExtensions);

				ExplicitKeyPropertiesCacheMethod = sqlMapperType.GetMethod("ExplicitKeyPropertiesCache", BindingFlags.Static | BindingFlags.NonPublic, Type.DefaultBinder, new[] { typeof(Type) }, null);
			}

			return (List<PropertyInfo>)ExplicitKeyPropertiesCacheMethod.Invoke(null, new[] { type });
		}

		public static List<PropertyInfo> ComputedPropertiesCache(Type type)
		{
			if (ComputedPropertiesCacheMethod == null)
			{
				var sqlMapperType = typeof(SqlMapperExtensions);

				ComputedPropertiesCacheMethod = sqlMapperType.GetMethod("ComputedPropertiesCache", BindingFlags.Static | BindingFlags.NonPublic, Type.DefaultBinder, new[] { typeof(Type) }, null);
			}

			return (List<PropertyInfo>)ComputedPropertiesCacheMethod.Invoke(null, new[] { type });
		}
	}
}
