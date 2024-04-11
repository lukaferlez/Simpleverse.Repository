using System;

namespace Simpleverse.Repository
{
	public static class ActionExtensions
	{
		public static T Get<T>(this Action<T> action) where T : class, new()
			=> Get(action, () => new T());

		public static T Get<T>(this Action<T> action, Func<T> createInstance)
		{
			var instance = createInstance();
			if (action == null)
				return instance;

			action(instance);
			return instance;
		}
	}
}
