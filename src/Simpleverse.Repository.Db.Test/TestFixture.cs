using Simpleverse.Repository.Db.Test.SqlServer;
using StackExchange.Profiling;
using System;
using Xunit;
using Xunit.Abstractions;
using System.Threading.Tasks;

namespace Simpleverse.Repository.Db.Test
{
	public class TestFixture : IClassFixture<DatabaseFixture>
	{
		protected readonly ITestOutputHelper _output;
		protected readonly DatabaseFixture _fixture;

		public TestFixture(DatabaseFixture fixture, ITestOutputHelper output)
		{
			_fixture = fixture;
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
			using(var profiler = Profile(profilerName))
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
