using StackExchange.Profiling;
using Xunit.Abstractions;

namespace Simpleverse.Repository.Db.Test
{
	public class ProfileWithOutput : Profile
	{
		private readonly ITestOutputHelper _output;

		public ProfileWithOutput(ITestOutputHelper output, string profilerName = null) : base(profilerName) 
		{
			_output = output;
		}

		public override void OverridableDispose()
		{
			base.OverridableDispose();
			_output.WriteLine(RenderProfiler(MiniProfiler.Current));
		}
	}
}
