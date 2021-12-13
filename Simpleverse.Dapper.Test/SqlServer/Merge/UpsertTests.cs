using Xunit;
using System.Linq;
using Dapper.Contrib.Extensions;
using Simpleverse.Dapper.SqlServer.Merge;

namespace Simpleverse.Dapper.Test.SqlServer.Merge
{
	[Collection("SqlServerCollection")]
	public class UpsertTests : IClassFixture<DatabaseFixture>
	{
		DatabaseFixture fixture;

		public UpsertTests(DatabaseFixture fixture)
		{
			this.fixture = fixture;
		}

		[Fact]
		public void UpsertAsyncTest()
		{
			using (var connection = fixture.GetConnection())
			{
				// arange
				connection.Open();
				connection.DeleteAll<ExplicitKey>();
				var record = TestData.ExplicitKeyData(1).FirstOrDefault();
				var inserted = connection.Insert(record);

				record.Name = (record.Id + 2).ToString();

				// act
				var updated = connection.UpsertAsync(record).Result;

				// assert
				var updatedRecords = connection.GetAll<ExplicitKey>();
				Assert.Equal(1, updated);
				Assert.Equal("3", updatedRecords.First(x => x.Id == 1).Name);
				var updatedRecord = updatedRecords.FirstOrDefault(x => x.Id == record.Id);
				Assert.NotNull(updatedRecord);
				Assert.Equal(record.Name, updatedRecord.Name);
			}
		}

		[Fact]
		public void UpsertMultipleAsyncTest()
		{
			using (var connection = fixture.GetConnection())
			{
				// arange
				connection.Open();
				connection.DeleteAll<ExplicitKey>();
				var records = TestData.ExplicitKeyData(10);
				var inserted = connection.Insert(records);
				records = records.Skip(1);
				foreach(var record in records)
				{
					record.Name = (record.Id + 2).ToString();
				}

				// act
				var updated = connection.UpsertBulkAsync(records).Result;

				// assert
				var updatedRecords = connection.GetAll<ExplicitKey>();
				Assert.Equal(9, updated);
				Assert.Equal("1", updatedRecords.First(x => x.Id == 1).Name);
				for (int i = 0; i < records.Count(); i++)
				{
					var record = records.ElementAt(i);
					var updatedRecord = updatedRecords.FirstOrDefault(x => x.Id == record.Id);
					Assert.NotNull(updatedRecord);
					Assert.Equal(record.Name, updatedRecord.Name);
				}
			}
		}
	}
}
