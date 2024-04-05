using Simpleverse.Repository.Db.Test.SqlServer;
using StackExchange.Profiling;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Simpleverse.Repository.Db.Test
{
	public class DatabaseTestFixture : TestFixture, IClassFixture<DatabaseFixture>
	{
		protected readonly DatabaseFixture _fixture;

		public DatabaseTestFixture(DatabaseFixture fixture, ITestOutputHelper output)
			: base(output)
		{
			_fixture = fixture;
		}
	}

	public class TestFixture
	{
		protected readonly ITestOutputHelper _output;

		public TestFixture(ITestOutputHelper output)
		{
			_output = output;
			MiniProfiler.StartNew();
		}

		public ProfileWithOutput Profile(string profilerName = null)
		{
			return new ProfileWithOutput(_output, profilerName);
		}

		public T Profile<T>(Func<T> profilerBlock)
			=> Profile(null, profilerBlock);

		public T Profile<T>(string profilerName, Func<T> profilerBlock)
		{
			using (var profiler = Profile(profilerName))
				return profilerBlock();
		}

		public Task<T> Profile<T>(Func<Task<T>> profilerBlock)
			=> ProfileAsync(null, profilerBlock);

		public Task<T> ProfileAsync<T>(string profilerName, Func<Task<T>> profilerBlock)
		{
			using (var profiler = Profile(profilerName))
				return profilerBlock();
		}
	}
}
