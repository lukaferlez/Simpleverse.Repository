using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Simpleverse.Repository.ChangeTracking
{
	public static class ChangeProxyFactory
	{
		// NOTES
		// https://sharplab.io/
		// https://learn.microsoft.com/en-us/dotnet/api/system.reflection.emit.opcodes?view=net//-8.0

		private static readonly ConcurrentDictionary<Type, Type> _typeCache = new ConcurrentDictionary<Type, Type>();

		public static T Create<T>()
		{
			Type typeOfT = typeof(T);

			if (_typeCache.TryGetValue(typeOfT, out Type changeTrackerType))
			{
				return (T)Activator.CreateInstance(changeTrackerType);
			}

			var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
				new AssemblyName { Name = typeOfT.Name },
				AssemblyBuilderAccess.Run
			);

			var moduleBuilder = assemblyBuilder.DefineDynamicModule(
				nameof(ChangeProxyFactory) + "." + typeOfT.Name
			);

			var builder = moduleBuilder
				.DefineType(
					typeOfT.Name + "_" + Guid.NewGuid(),
					TypeAttributes.Public | TypeAttributes.Class
				);

			var onChangeMethod = builder.AddIChangeTrackImplementation(typeOfT);
			builder.AddTargetTypeImplementation(typeOfT, onChangeMethod);

#if NETSTANDARD2_0
			var generatedType = typeBuilder.CreateTypeInfo().AsType();
#else
			var generatedType = builder.CreateType();
#endif

			_typeCache.TryAdd(typeOfT, generatedType);
			return (T)Activator.CreateInstance(generatedType);
		}

		#region ChangeTrack

		private static MethodInfo AddIChangeTrackImplementation(this TypeBuilder builder, Type typeOfT)
		{
			var typeofIChangeTrack = typeof(IChangeTrack);
			builder.AddInterfaceImplementation(typeofIChangeTrack);

			var typeOfChangeTrack = typeof(ChangeTrack);
			var changesFieldInfo = builder.DefineField("_changes", typeOfChangeTrack, FieldAttributes.Private);

			builder.AddConstructor(typeOfT, changesFieldInfo);
			builder.AddChangesFieldRedirectProperty(changesFieldInfo, typeOfChangeTrack.GetProperty(nameof(IChangeTrack.IsChanged)));
			builder.AddChangesFieldRedirectProperty(changesFieldInfo, typeOfChangeTrack.GetProperty(nameof(IChangeTrack.Changed)));
			var trackMethod = builder.AddSetChangedMethod(changesFieldInfo);
			builder.AddClearMethod(changesFieldInfo);

			return trackMethod;
		}

		private static void AddConstructor(this TypeBuilder builder, Type typeOfT, FieldInfo changesFieldInfo)
		{
			var constructor = builder.DefineConstructor(
				MethodAttributes.Public,
				CallingConventions.Standard,
				Type.EmptyTypes
			);

			var emitter = constructor.GetILGenerator();
			emitter.Emit(OpCodes.Nop);
			if (!typeOfT.IsInterface)
			{
				var parentConstructor = typeOfT.GetConstructors().FirstOrDefault(x => x.GetParameters().Length == 0);
				if (parentConstructor == null)
					throw new NotSupportedException("Class proxy type must have a parameterless constructor.");

				emitter.Emit(OpCodes.Ldarg_0);
				emitter.Emit(OpCodes.Call, parentConstructor);
			}
			emitter.Emit(OpCodes.Ldarg_0);
			emitter.Emit(OpCodes.Newobj, typeof(ChangeTrack).GetConstructor(Type.EmptyTypes));
			emitter.Emit(OpCodes.Stfld, changesFieldInfo);
			emitter.Emit(OpCodes.Ret);
		}

		private static void AddChangesFieldRedirectProperty(this TypeBuilder builder, FieldInfo changesField, PropertyInfo propertyInfo)
		{
			var property = builder.DefineProperty(
				propertyInfo.Name,
				PropertyAttributes.None,
				propertyInfo.PropertyType,
				propertyInfo.CanWrite ? new Type[] { propertyInfo.PropertyType } : Type.EmptyTypes
			);

			const MethodAttributes getSetAttr =
				MethodAttributes.Public
				| MethodAttributes.Virtual
				| MethodAttributes.HideBySig;

			var currGetPropMthdBldr = builder
				.DefineMethod(
					"get_" + property.Name,
					getSetAttr,
					property.PropertyType,
					Type.EmptyTypes
				);

			var currGetIl = currGetPropMthdBldr.GetILGenerator();
			currGetIl.Emit(OpCodes.Ldarg_0);
			currGetIl.Emit(OpCodes.Ldfld, changesField);
			currGetIl.Emit(OpCodes.Callvirt, propertyInfo.GetGetMethod());
			currGetIl.Emit(OpCodes.Ret);
		}

		private static MethodInfo AddSetChangedMethod(this TypeBuilder builder, FieldInfo changesField)
		{
			const MethodAttributes getSetAttr =
				MethodAttributes.Private
				| MethodAttributes.Virtual
				| MethodAttributes.HideBySig;

			var method = builder.DefineMethod(
				nameof(ChangeTrack.SetChanged),
				getSetAttr,
				typeof(void),
				new[] { typeof(string) }
			);

			var ilGen = method.GetILGenerator();
			ilGen.Emit(OpCodes.Ldarg_0);
			ilGen.Emit(OpCodes.Ldfld, changesField);
			ilGen.Emit(OpCodes.Ldarg_1);
			ilGen.Emit(OpCodes.Callvirt, changesField.FieldType.GetMethod(nameof(ChangeTrack.SetChanged)));
			ilGen.Emit(OpCodes.Ret);

			return method;
		}

		private static void AddClearMethod(this TypeBuilder builder, FieldInfo changesField)
		{
			const MethodAttributes getSetAttr =
				MethodAttributes.Public
				| MethodAttributes.Virtual
				| MethodAttributes.HideBySig;

			var method = builder.DefineMethod(
				nameof(IChangeTrack.Clear),
				getSetAttr,
				typeof(void),
				Type.EmptyTypes
			);

			var ilGen = method.GetILGenerator();
			ilGen.Emit(OpCodes.Ldarg_0);
			ilGen.Emit(OpCodes.Ldfld, changesField);
			ilGen.Emit(OpCodes.Callvirt, changesField.FieldType.GetMethod(nameof(IChangeTrack.Clear)));
			ilGen.Emit(OpCodes.Ret);
		}

		#endregion

		#region ProxyImplementation

		private static void AddTargetTypeImplementation(this TypeBuilder typeBuilder, Type typeOfT, MethodInfo onChangeMethod)
		{
			var properties = ExtractProperties(typeOfT);

			if (typeOfT.IsInterface)
			{
				if (properties.Any(x => !x.CanWrite))
					throw new NotSupportedException("Non write interface properties are not supported.");

				typeBuilder.AddInterfaceImplementation(typeOfT);
			}
			else
			{
				if (Settings.ForceUseOfVirtualProperties)
				{
					if (properties.Any(x => x.CanWrite && (!x.SetMethod.IsVirtual || x.SetMethod.IsFinal)))
						throw new NotSupportedException("Non virtual or final write properties are not supported.");
				}

				typeBuilder.SetParent(typeOfT);
			}

			foreach (var property in properties)
			{
				if (typeOfT.IsInterface)
					typeBuilder.AddInterfaceProxyProperty(typeOfT, property, onChangeMethod);
				else
					typeBuilder.AddProxyProperty(typeOfT, property, onChangeMethod);
			}
		}

		private static IEnumerable<PropertyInfo> ExtractProperties(Type type)
		{
			if (!type.IsInterface)
				return type.GetProperties();

			var interfaceTypes = ExtractInterfaces(type);

			var properties = interfaceTypes
				.SelectMany(x => x.GetProperties())
				.GroupBy(x => x.Name)
				.ToArray();

			var duplicateProperties = properties
				.Where(x => x.GroupBy(x => x.PropertyType).Count() > 1)
				.SelectMany(x => x)
				.ToArray();
			if (duplicateProperties.Any())
			{
				var ex = new NotSupportedException("Multiple properties with the same name are not supported");
				ex.Data.Add("DuplicateProperties", duplicateProperties.Select(x => x.DeclaringType.Name + "." + x.Name));
				throw ex;
			}

			return properties.Select(x => x.First());
		}

		private static IEnumerable<Type> ExtractInterfaces(Type type)
		{
			var interfaceTypes = new List<Type>();
			foreach (var typeOfInterface in type.GetInterfaces())
			{
				interfaceTypes.AddRange(ExtractInterfaces(typeOfInterface));
			}

			if (type.IsInterface)
				interfaceTypes.Add(type);

			return interfaceTypes;
		}

		private static void AddInterfaceProxyProperty(this TypeBuilder typeBuilder, Type typeOfT, PropertyInfo sourceProperty, MethodInfo onChangeMethod)
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
			EmitTrackChange(setMethodBody, propertyName, propertyType, onChangeMethod);
			setMethodBody.Emit(OpCodes.Ret);

			property.SetSetMethod(setMethod);
			typeBuilder.DefineMethodOverride(setMethod, sourceProperty.SetMethod);
		}

		private static void AddProxyProperty(this TypeBuilder typeBuilder, Type typeOfT, PropertyInfo sourceProperty, MethodInfo onChangeMethod)
		{
			if (!sourceProperty.CanWrite)
				return;

			if (!sourceProperty.SetMethod.IsVirtual)
				return;

			if (sourceProperty.SetMethod.IsFinal)
				return;

			var propertyName = sourceProperty.Name;
			var propertyType = sourceProperty.PropertyType;

			const MethodAttributes getSetAttr =
				MethodAttributes.Public
				| MethodAttributes.Virtual
				| MethodAttributes.HideBySig;

			var getMethod = typeBuilder.DefineMethod(
				"override_get_" + propertyName,
				getSetAttr,
				propertyType,
				Type.EmptyTypes
			);

			var getMethodBody = getMethod.GetILGenerator();
			getMethodBody.Emit(OpCodes.Ldarg_0);
			getMethodBody.Emit(OpCodes.Call, sourceProperty.GetMethod);
			getMethodBody.Emit(OpCodes.Ret);

			typeBuilder.DefineMethodOverride(getMethod, sourceProperty.GetMethod);

			var setMethod = typeBuilder.DefineMethod(
				"overriade_set_" + propertyName,
				getSetAttr,
				null,
				new[] { propertyType }
			);

			// store value in private field and set the isdirty flag
			var setMethodBody = setMethod.GetILGenerator();
			setMethodBody.Emit(OpCodes.Ldarg_0);
			setMethodBody.Emit(OpCodes.Ldarg_1);
			setMethodBody.Emit(OpCodes.Call, sourceProperty.SetMethod);
			EmitTrackChange(setMethodBody, propertyName, propertyType, onChangeMethod);
			setMethodBody.Emit(OpCodes.Ret);

			typeBuilder.DefineMethodOverride(setMethod, sourceProperty.SetMethod);
		}

		private static void EmitTrackChange(ILGenerator setMethodIl, string propertyName, Type propertyType, MethodInfo onChangeMethod)
		{
			setMethodIl.Emit(OpCodes.Ldarg_0);
			setMethodIl.Emit(OpCodes.Ldstr, propertyName);
			setMethodIl.Emit(OpCodes.Call, onChangeMethod);
		}

		#endregion
	}
}
