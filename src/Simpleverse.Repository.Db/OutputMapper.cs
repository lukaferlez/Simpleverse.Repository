using Microsoft.Extensions.Logging;
using Simpleverse.Repository.Db.Entity;
using Simpleverse.Repository.Db.Meta;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Simpleverse.Repository.Db
{
	public class OutputMapper
	{
		private sealed class EntityState<T>
		{
			public T Entity { get; }
			public bool Mapped { get; set; }

			public EntityState(T entity)
			{
				this.Entity = entity;
			}
		}

		public static void MapOnce<T>(
			IEnumerable<T> entities,
			IEnumerable<T> results,
			IEnumerable<PropertyInfo> propertiesToMatch,
			IEnumerable<PropertyInfo> propertiesToMap
		)
			=> Map(entities, results, propertiesToMatch, propertiesToMap, true);

		public static void Map<T>(
			IEnumerable<T> entities,
			IEnumerable<T> results,
			IEnumerable<PropertyInfo> propertiesToMatch,
			IEnumerable<PropertyInfo> propertiesToMap
		)
			=> Map(entities, results, propertiesToMatch, propertiesToMap, false);

		private static void Map<T>(
			IEnumerable<T> entities,
			IEnumerable<T> results,
			IEnumerable<PropertyInfo> propertiesToMatch,
			IEnumerable<PropertyInfo> propertiesToMap,
			bool mapResultOnce
		)
		{
			if (results == null || !results.Any())
				return;

			var entitiesToMap = (IEnumerable)entities;
			var resultsToMap = (IEnumerable)results;

			if (TypeMeta.Get<T>().IsProjection)
			{
				entitiesToMap = entities.Select(x => ((IProject<object>)x).Model);
				resultsToMap = results.Select(x => ((IProject<object>)x).Model);
			}

			Map(
				entitiesToMap,
				resultsToMap,
				propertiesToMatch,
				propertiesToMap,
				mapResultOnce
			);
		}

		private static void Map(
			IEnumerable entities,
			IEnumerable results,
			IEnumerable<PropertyInfo> propertiesToMatch,
			IEnumerable<PropertyInfo> propertiesToMap,
			bool mapResultOnce
		)
		{
			var logger = Settings.GetLogger<OutputMapper>();

			var entitiesWithMapping = new List<EntityState<object>>();
			foreach (var entity in entities)
			{
				entitiesWithMapping.Add(new EntityState<object>(entity));
			}

			foreach (var result in results)
			{
				for (var iCount = 0; iCount < entitiesWithMapping.Count; iCount++)
				{
					var entity = entitiesWithMapping[iCount];
					if (entity.Mapped)
						continue;

					var found = true;
					foreach (var property in propertiesToMatch)
					{
						var entityValue = property.GetValue(entity.Entity);
						var resultValue = property.GetValue(result);

						if (!IsEqual(entityValue, resultValue))
						{
							found = false;
							break;
						}
					}

					if (found)
					{
						foreach (var property in propertiesToMap)
						{
							property.SetValue(entity.Entity, property.GetValue(result));
						}
						entity.Mapped = true;

						if (mapResultOnce)
							break;
					}
					else
					{
						logger.LogDebug(
							"Failed to map return values to objects. Entity: {Entity}, Result: {result}",
							entity.Entity,
							result
						);
					}
				}
			}
		}

		private static bool IsEqual(object entityValue, object resultValue)
		{
			if (entityValue == null && resultValue == null)
				return true;

			if (entityValue != null && entityValue.Equals(resultValue))
				return true;

			if (resultValue != null && resultValue.Equals(entityValue))
				return true;

			if (entityValue != null && resultValue != null)
			{
				var type = entityValue.GetType();
				if (type.IsEnum && Enum.IsDefined(type, resultValue))
					return true;

				if (entityValue is DateTime && resultValue is DateTime)
					return true;
			}

			return false;
		}
	}
}
