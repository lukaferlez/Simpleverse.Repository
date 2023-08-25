using Simpleverse.Repository.Db.Test.SqlServer;
using StackExchange.Profiling;
using StackExchange.Profiling.Internal;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System;
using Xunit;
using Xunit.Abstractions;
using System.Linq;
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

	public class ProfileWithOutput : IDisposable
	{
		private readonly ITestOutputHelper _output;

		public ProfileWithOutput(ITestOutputHelper output, string profilerName = null)
		{
			_output = output;
			MiniProfiler.StartNew(profilerName);
		}

		public void Dispose()
		{
			MiniProfiler.Current.Stop();
			_output.WriteLine(CustomRenderPlainText(MiniProfiler.Current));
		}

		private string CustomRenderPlainText(MiniProfiler profiler, bool htmlEncode = false)
		{
			if (profiler == null) return string.Empty;

			var text = StringBuilderCache.Get()
				.Append(htmlEncode ? WebUtility.HtmlEncode(Environment.MachineName) : Environment.MachineName)
				.Append(" at ")
				.Append(DateTime.UtcNow)
				.AppendLine();

			var timings = new Stack<Timing>();
			timings.Push(profiler.Root);

			while (timings.Count > 0)
			{
				var timing = timings.Pop();

				text.AppendFormat("{0} {1} = {2:###,##0.##}[ms]",
					new string('>', timing.Depth),
					htmlEncode ? WebUtility.HtmlEncode(timing.Name) : timing.Name,
					timing.DurationMilliseconds);

				if (timing.HasCustomTimings)
				{
					// TODO: Customize this code block.

					// Custom timings grouped by category. Collect all custom timings in a list.
					var customTimingsFlat = new List<KeyValuePair<string, CustomTiming>>(capacity: timing.CustomTimings.Sum(ct => ct.Value.Count));
					foreach (var pair in timing.CustomTimings)
					{
						var type = pair.Key;
						var customTimings = pair.Value;

						customTimingsFlat.AddRange(pair.Value.Select(ct => KeyValuePair.Create(type, ct)));
						text.AppendFormat(" ({0} = {1:###,##0.##}[ms] in {2} cmd{3})",
							type,
							customTimings.Sum(ct => ct.DurationMilliseconds),
							customTimings.Count,
							customTimings.Count == 1 ? string.Empty : "s");
					}

					foreach (var pair in customTimingsFlat.OrderBy(kvp => kvp.Value.StartMilliseconds))
					{
						var type = pair.Key;
						var ct = pair.Value;

						text.AppendLine();
						var mainPart = string.Format(
							"{0}{1} +{2:###,##0.##}[ms] {3:###,##0.##}[ms] ",
							new string(' ', timing.Depth + 2),
							type,
							ct.StartMilliseconds,
							ct.DurationMilliseconds
						);
						text.Append(mainPart);
						text.AppendLine();
						// Shift command text to closer to the command for better readability.
						var prefix = new string(' ', 4);
						string cmdLine = null;
						using (var reader = new StringReader(ct.CommandString))
						{
							while ((cmdLine = reader.ReadLine()) != null)
							{
								text.Append(prefix);
								text.Append(cmdLine);
								if (reader.Peek() == -1 && profiler.Options.ExcludeStackTraceSnippetFromCustomTimings)
								{
									break;
								}
								text.AppendLine();
							}
						}

						//if (profiler.Options.ExcludeStackTraceSnippetFromCustomTimings)
						//{
						//	continue;
						//}
						//text.Append(ct.StackTraceSnippet);
					}
				}

				text.AppendLine();

				if (timing.HasChildren)
				{
					var children = timing.Children;
					for (var i = children.Count - 1; i >= 0; i--) timings.Push(children[i]);
				}
			}

			return text.ToStringRecycle();
		}
	}
}
