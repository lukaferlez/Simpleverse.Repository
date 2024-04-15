using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Simpleverse.Repository.ChangeTracking
{
	public static class TypeFactory
	{
		// NOTES
		// https://sharplab.io/
		// https://learn.microsoft.com/en-us/dotnet/api/system.reflection.emit.opcodes?view=net//-8.0

		private static readonly ConcurrentDictionary<Type, Type> _typeCache = new ConcurrentDictionary<Type, Type>();

		public static T Create<T>(params object[] constructorParams)
			where T : class
		{
			var type = GetType<T>();
			return (T)Activator.CreateInstance(type, constructorParams);
		}

		private static Type GetType<T>()
		{
			Type typeOfT = typeof(T);
			if (typeOfT.IsClass)
				return typeOfT;

			if (!typeOfT.IsInterface)
				throw new NotSupportedException("Only class or interface types are supported.");

			Type compositeType = null;
			if (_typeCache.TryGetValue(typeOfT, out compositeType))
				return compositeType;

			var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
				new AssemblyName { Name = typeOfT.Name },
				AssemblyBuilderAccess.Run
			);

			var moduleBuilder = assemblyBuilder.DefineDynamicModule(
				nameof(TypeFactory) + "." + typeOfT.Name
			);

			var builder = moduleBuilder
				.DefineType(
					typeOfT.Name + "_" + Guid.NewGuid(),
					TypeAttributes.Public | TypeAttributes.Class
				);

			builder.AddTargetTypeImplementation(typeOfT);

#if NETSTANDARD2_0
			compositeType = typeBuilder.CreateTypeInfo().AsType();
#else
			compositeType = builder.CreateType();
#endif

			_typeCache.TryAdd(typeOfT, compositeType);
			return compositeType;
		}

		#region TypeImplementation

		private static void AddTargetTypeImplementation(this TypeBuilder typeBuilder, Type typeOfT)
		{
			typeBuilder.AddInterfaceImplementation(typeOfT);

			var properties = ExtractProperties(typeOfT);
			foreach (var property in properties)
			{
				typeBuilder.ImplementProperty(property);
			}
		}

		private static IEnumerable<PropertyInfo> ExtractProperties(Type type)
		{
			var interfaceTypes = ExtractInterfaces(type);
			return
				interfaceTypes
					.SelectMany(x => x.GetProperties())
					.GroupBy(x => x.Name)
					.Select(x => x.First());
		}

		private static IEnumerable<Type> ExtractInterfaces(Type type)
		{
			var interfaceTypes = new List<Type>(type.GetInterfaces());
			foreach (var typeOfInterface in interfaceTypes)
				interfaceTypes.AddRange(ExtractInterfaces(typeOfInterface));

			return interfaceTypes;
		}

		private static void ImplementProperty(this TypeBuilder typeBuilder, PropertyInfo sourceProperty)
		{
			var propertyName = sourceProperty.Name;
			var propertyType = sourceProperty.PropertyType;

			var field = typeBuilder.DefineField("_" + propertyName, propertyType, FieldAttributes.Private);

			var property = typeBuilder.DefineProperty(
				propertyName,
				PropertyAttributes.None,
				propertyType,
				new[] { propertyType }
			);

			const MethodAttributes getSetAttr =
				MethodAttributes.Public
				| MethodAttributes.Virtual
				| MethodAttributes.HideBySig;

			var getMethod = typeBuilder.DefineMethod(
				"get_" + propertyName,
				getSetAttr,
				propertyType,
				Type.EmptyTypes
			);

			var getMethodBody = getMethod.GetILGenerator();
			getMethodBody.Emit(OpCodes.Ldarg_0);
			getMethodBody.Emit(OpCodes.Ldfld, field);
			getMethodBody.Emit(OpCodes.Ret);

			property.SetGetMethod(getMethod);
			typeBuilder.DefineMethodOverride(getMethod, sourceProperty.GetMethod);

			var setMethod = typeBuilder.DefineMethod(
				"set_" + propertyName,
				getSetAttr,
				null,
				new[] { propertyType }
			);

			var setMethodBody = setMethod.GetILGenerator();
			setMethodBody.Emit(OpCodes.Ldarg_0);
			setMethodBody.Emit(OpCodes.Ldarg_1);
			setMethodBody.Emit(OpCodes.Stfld, field);
			setMethodBody.Emit(OpCodes.Ret);

			property.SetSetMethod(setMethod);
			typeBuilder.DefineMethodOverride(setMethod, sourceProperty.SetMethod);
		}

		#endregion
	}
}
