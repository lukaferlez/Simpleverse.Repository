using Xunit;
using System.Linq;
using Dapper.Contrib.Extensions;
using Simpleverse.Dapper.SqlServer;

namespace Simpleverse.Dapper.Test.SqlServer
{
	[Collection("SqlServerCollection")]
	public class GetTests : IClassFixture<DatabaseFixture>
	{
		DatabaseFixture fixture;

		public GetTests(DatabaseFixture fixture)
		{
			this.fixture = fixture;
		}

		[Fact]
		public void GetAsyncTest()
		{
			using (var connection = fixture.GetConnection())
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
				for (int i = 0; i < records.Count(); i++)
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
