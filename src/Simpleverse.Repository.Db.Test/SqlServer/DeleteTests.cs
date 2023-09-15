using Xunit;
using System.Linq;
using Dapper.Contrib.Extensions;
using Simpleverse.Repository.Db.SqlServer;
using Simpleverse.Repository.Db.Extensions;
using Xunit.Abstractions;

namespace Simpleverse.Repository.Db.Test.SqlServer
{
	[Collection("SqlServerCollection")]
	public class DeleteTests : TestFixture
	{
		public DeleteTests(DatabaseFixture fixture, ITestOutputHelper output)
			: base(fixture, output)
		{
		}

		[Fact]
		public void DeleteAsyncTest()
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
				var deleted = connection.DeleteBulkAsync(records).Result;

				// assert
				Assert.Equal(5, deleted);
				var remaningRecords = connection.GetAll<ExplicitKey>();
				Assert.Equal(5, remaningRecords.Count());
				foreach (var record in records)
				{
					var remaningRecord = remaningRecords.FirstOrDefault(x => x.Id == record.Id);
					Assert.Null(remaningRecord);
				}
			}
		}
	}
}
