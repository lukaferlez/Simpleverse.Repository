using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Simpleverse.Repository
{
	public static class Settings
	{
		public static bool ForceUseOfVirtualProperties { get; set; } = true;

		public static ILoggerFactory LoggerFactory { get; set; }

		public static ILogger GetLogger<T>()
		{
			if (LoggerFactory == null)
				return NullLogger.Instance;

			return LoggerFactory.CreateLogger<T>();
		}
	}
}
