using Xunit;
using System.Linq;
using Dapper.Contrib.Extensions;
using Simpleverse.Dapper.SqlServer.Merge;
using Simpleverse.Dapper.SqlServer;
using System;

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
		public void UpsertAsyncExplicitKeyTest()
		{
			using (var connection = fixture.GetConnection())
			{
				// arange
				connection.Open();
				connection.Truncate<ExplicitKey>();
				var record = TestData.ExplicitKeyData(1).FirstOrDefault();
				var inserted = connection.Insert(record);

				record.Name = (record.Id + 2).ToString();

				// act
				var updated = connection.UpsertAsync(record).Result;

				// assert
				var updatedRecords = connection.GetAll<ExplicitKey>();
				Assert.Equal(1, updated);
				var updatedRecord = updatedRecords.FirstOrDefault(x => x.Id == record.Id);
				Assert.NotNull(updatedRecord);
				Assert.Equal("3", updatedRecord.Name);
				Assert.Equal(record.Name, updatedRecord.Name);
			}
		}

		[Fact]
		public void UpsertAsyncIdentityTest()
		{
			using (var connection = fixture.GetConnection())
			{
				// arange
				connection.Open();
				connection.Truncate<Identity>();
				var record = TestData.IdentityWithIdData(1).FirstOrDefault();
				var inserted = connection.Insert(record);

				record.Name = (record.Id + 2).ToString();

				// act
				var updated = connection.UpsertAsync(record).Result;

				// assert
				var updatedRecords = connection.GetAll<Identity>();
				Assert.Equal(1, updated);
				var updatedRecord = updatedRecords.FirstOrDefault(x => x.Id == record.Id);
				Assert.NotNull(updatedRecord);
				Assert.Equal("3", updatedRecord.Name);
				Assert.Equal(record.Name, updatedRecord.Name);
			}
		}

		[Fact]
		public void UpsertAsyncWriteAttributeTest()
		{
			using (var connection = fixture.GetConnection())
			{
				// arange
				connection.Open();
				connection.Truncate<Write>();
				var record = TestData.WriteData(1).FirstOrDefault();
				var inserted = connection.Insert(record);

				record.Ignored = record.Id + 2;
				record.NotIgnored = record.Id + 2;

				// act
				var updated = connection.UpsertAsync(record).Result;

				// assert
				var updatedRecords = connection.GetAll<Write>();
				Assert.Equal(1, updated);
				var updatedRecord = updatedRecords.FirstOrDefault(x => x.Id == record.Id);
				Assert.NotNull(updatedRecord);
				Assert.Null(updatedRecord.Ignored);
				Assert.Equal(3, updatedRecord.NotIgnored);
				Assert.Equal(record.NotIgnored, updatedRecord.NotIgnored);
			}
		}

		[Fact]
		public void UpsertAsyncComputedAttributeTest()
		{
			using (var connection = fixture.GetConnection())
			{
				// arange
				connection.Open();
				connection.Truncate<Computed>();
				var record = TestData.ComputedData(1).FirstOrDefault();
				var inserted = connection.Insert(record);

				record.Name = (record.Id + 2).ToString();
				record.Value = record.Value + 2;
				record.ValueDate = record.ValueDate.AddDays(30);

				// act
				var updated = connection.UpsertAsync(record).Result;

				// assert
				var updatedRecords = connection.GetAll<Computed>();
				Assert.Equal(1, updated);
				var updatedRecord = updatedRecords.FirstOrDefault(x => x.Id == record.Id);
				Assert.NotNull(updatedRecord);
				Assert.Equal(record.Name, updatedRecord.Name);
				Assert.Equal(5, updatedRecord.Value);
				Assert.Equal(new DateTime(2022, 05, 02), updatedRecord.ValueDate);
			}
		}

		[Fact]
		public void UpsertBulkAsyncExplicitTest()
		{
			using (var connection = fixture.GetConnection())
			{
				// arange
				connection.Open();
				connection.Truncate<ExplicitKey>();
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

		[Fact]
		public void UpsertBulkAsyncIdentityTest()
		{
			using (var connection = fixture.GetConnection())
			{
				// arange
				connection.Open();
				connection.Truncate<Identity>();
				var records = TestData.IdentityWithIdData(10);
				var inserted = connection.Insert(records);
				records = records.Skip(1);
				foreach (var record in records)
				{
					record.Name = (record.Id + 2).ToString();
				}

				// act
				var updated = connection.UpsertBulkAsync(records).Result;

				// assert
				var updatedRecords = connection.GetAll<Identity>();
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

		[Fact]
		public void UpsertBulkAsyncWriteAttributeTest()
		{
			using (var connection = fixture.GetConnection())
			{
				// arange
				connection.Open();
				connection.Truncate<Write>();
				var records = TestData.WriteData(10);
				var inserted = connection.Insert(records);
				records = records.Skip(1);
				foreach (var record in records)
				{
					record.Ignored = (record.Id + 2);
					record.NotIgnored = (record.Id + 3);
				}

				// act
				var updated = connection.UpsertBulkAsync(records).Result;

				// assert
				var updatedRecords = connection.GetAll<Write>();
				Assert.Equal(9, updated);
				Assert.Null(updatedRecords.First(x => x.Id == 1).Ignored);
				Assert.Equal(100, updatedRecords.First(x => x.Id == 1).NotIgnored);
				for (int i = 0; i < records.Count(); i++)
				{
					var record = records.ElementAt(i);
					var updatedRecord = updatedRecords.FirstOrDefault(x => x.Id == record.Id);
					Assert.NotNull(updatedRecord);
					Assert.Null(updatedRecord.Ignored);
					Assert.Equal(record.NotIgnored, updatedRecord.NotIgnored);
				}
			}
		}

		[Fact(Skip = "Doesn't work without key properties")]
		public void UpsertBulkAsyncComputedAttributeTest()
		{
			using (var connection = fixture.GetConnection())
			{
				// arange
				connection.Open();
				connection.Truncate<Computed>();
				var records = TestData.ComputedData(10);
				var inserted = connection.Insert(records);
				records = records.Skip(1);
				foreach (var record in records)
				{
					record.Name = (record.Id + 2).ToString();
					record.Value = record.Value + 2;
					record.ValueDate = record.ValueDate.AddDays(30);
				}

				// act
				var updated = connection.UpsertAsync(records).Result;

				// assert
				var updatedRecords = connection.GetAll<Computed>();
				Assert.Equal(9, updated);
				Assert.Equal("1", updatedRecords.First(x => x.Id == 1).Name);
				for (int i = 0; i < records.Count(); i++)
				{
					var record = records.ElementAt(i);
					var updatedRecord = updatedRecords.FirstOrDefault(x => x.Id == record.Id);
					Assert.NotNull(updatedRecord);
					Assert.Equal(record.Name, updatedRecord.Name);
					Assert.Equal(5, updatedRecord.Value);
					Assert.Equal(new DateTime(2022, 05, 02), updatedRecord.ValueDate);
				}
			}
		}
	}
}
