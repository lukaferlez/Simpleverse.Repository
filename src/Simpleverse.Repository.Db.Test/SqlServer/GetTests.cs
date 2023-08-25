using Xunit;
using System.Linq;
using Dapper.Contrib.Extensions;
using Simpleverse.Repository.Db.SqlServer;
using Simpleverse.Repository.Db.Extensions;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Xunit.Abstractions;

namespace Simpleverse.Repository.Db.Test.SqlServer
{
    [Collection("SqlServerCollection")]
	public class GetTests : TestFixture
	{
		public GetTests(DatabaseFixture fixture, ITestOutputHelper output)
			: base(fixture, output)
		{
		}

		[Fact]
		public void GetAsyncTest()
		{
			using (var profiler = Profile())
			using (var connection = _fixture.GetConnection())
			{
				// arange
				connection.Open();
				connection.Truncate<ExplicitKey>();
				var records = TestData.ExplicitKeyData(10);
				var inserted = connection.Insert(records);
				records = records.Skip(2).Take(5);

				// act
				var fetched = connection.GetBulkAsync(records).Result;

				// assert
				Assert.Equal(5, fetched.Count());
				for (var i = 0; i < records.Count(); i++)
				{
					var record = records.ElementAt(i);
					var fetchedRecord = fetched.FirstOrDefault(x => x.Id == record.Id);
					Assert.NotNull(fetchedRecord);
					Assert.Equal(record.Id, record.Id);
					Assert.Equal(record.Name, record.Name);
				}
			}
		}
	}
}
