using System;

namespace Simpleverse.Repository
{
	public static class ActionExtensions
	{
		public static T Get<T>(this Action<T> action) where T : class, new()
		{
			var instance = new T();
			if (action == null)
				return instance;

			action(instance);
			return instance;
		}
	}
}
