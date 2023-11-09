using Xunit;
using System.Linq;
using Dapper.Contrib.Extensions;
using System;
using Simpleverse.Repository.Db.SqlServer;
using Simpleverse.Repository.Db.SqlServer.Merge;
using Simpleverse.Repository.Db.Extensions;
using Xunit.Abstractions;

namespace Simpleverse.Repository.Db.Test.SqlServer.Merge
{
    [Collection("SqlServerCollection")]
	public class UpsertTests : TestFixture
	{
		public UpsertTests(DatabaseFixture fixture, ITestOutputHelper output)
			: base(fixture, output)
		{
		}

		[Fact]
		public void UpsertAsyncExplicitKeyTest()
		{
			using (var profiler = Profile())
			using (var connection = _fixture.GetConnection())
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
			using (var profiler = Profile())
			using (var connection = _fixture.GetConnection())
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
			using (var profiler = Profile())
			using (var connection = _fixture.GetConnection())
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
			using (var profiler = Profile())
			using (var connection = _fixture.GetConnection())
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
			using (var profiler = Profile())
			using (var connection = _fixture.GetConnection())
			{
				// arange
				connection.Open();
				connection.Truncate<ExplicitKey>();
				var records = TestData.ExplicitKeyData(10);
				var inserted = connection.Insert(records);
				records = records.Skip(1);
				foreach (var record in records)
				{
					record.Name = (record.Id + 2).ToString();
				}

				// act
				var updated = connection.UpsertBulkAsync(records).Result;

				// assert
				var updatedRecords = connection.GetAll<ExplicitKey>();
				Assert.Equal(9, updated);
				Assert.Equal("1", updatedRecords.First(x => x.Id == 1).Name);
				for (var i = 0; i < records.Count(); i++)
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
			using (var profiler = Profile())
			using (var connection = _fixture.GetConnection())
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
				for (var i = 0; i < records.Count(); i++)
				{
					var record = records.ElementAt(i);
					var updatedRecord = updatedRecords.FirstOrDefault(x => x.Id == record.Id);
					Assert.NotNull(updatedRecord);
					Assert.Equal(record.Name, updatedRecord.Name);
				}
			}
		}

		[Fact]
		public void UpsertBulkAsyncIdentityWithMapGeneratedValuesTest()
		{
			using (var profiler = Profile())
			using (var connection = _fixture.GetConnection())
			{
				// arange
				connection.Open();
				connection.Truncate<Identity>();
				var records = TestData.IdentityWithoutIdData(10);
				var recordsToInsert = records.Take(9);

				var inserted = connection.InsertBulkAsync(recordsToInsert, outputMap: OutputMapper.MapOnce).Result;
				foreach (var record in recordsToInsert.Skip(1))
				{
					record.Name = (record.Id + 2).ToString();
				}

				// act
				var updated = connection.UpsertBulkAsync(records.Skip(1), outputMap: OutputMapper.Map).Result;

				// assert
				var updatedRecords = connection.GetAll<Identity>();
				Assert.Equal(9, updated);
				Assert.Equal("1", updatedRecords.First(x => x.Id == 1).Name);
				for (var i = 0; i < records.Count(); i++)
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
			using (var profiler = Profile())
			using (var connection = _fixture.GetConnection())
			{
				// arange
				connection.Open();
				connection.Truncate<Write>();
				var records = TestData.WriteData(10);
				var inserted = connection.Insert(records);
				records = records.Skip(1);
				foreach (var record in records)
				{
					record.Ignored = record.Id + 2;
					record.NotIgnored = record.Id + 3;
				}

				// act
				var updated = connection.UpsertBulkAsync(records).Result;

				// assert
				var updatedRecords = connection.GetAll<Write>();
				Assert.Equal(9, updated);
				Assert.Null(updatedRecords.First(x => x.Id == 1).Ignored);
				Assert.Equal(100, updatedRecords.First(x => x.Id == 1).NotIgnored);
				for (var i = 0; i < records.Count(); i++)
				{
					var record = records.ElementAt(i);
					var updatedRecord = updatedRecords.FirstOrDefault(x => x.Id == record.Id);
					Assert.NotNull(updatedRecord);
					Assert.Null(updatedRecord.Ignored);
					Assert.Equal(record.NotIgnored, updatedRecord.NotIgnored);
				}
			}
		}

		[Fact]
		public void UpsertBulkAsyncComputedAttributeTest()
		{
			using (var profiler = Profile())
			using (var connection = _fixture.GetConnection())
			{
				// arange
				connection.Open();
				connection.Truncate<Computed>();
				var records = TestData.ComputedData(10);
				var inserted = connection.InsertBulkAsync(records, outputMap: OutputMapper.MapOnce).Result;
				records = records.Skip(1);
				foreach (var record in records)
				{
					record.Name = (record.Id + 2).ToString();
					record.Value = record.Value + 2;
					record.ValueDate = record.ValueDate.AddDays(30);
				}

				// act
				var updated = connection.UpsertBulkAsync(records).Result;

				// assert
				var updatedRecords = connection.GetAll<Computed>();
				Assert.Equal(9, updated);
				Assert.Equal("1", updatedRecords.First(x => x.Id == 1).Name);
				for (var i = 0; i < records.Count(); i++)
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

		[Fact]
		public void UpsertBulkAsyncComputedWithMapGeneratedValuesAttributeTest()
		{
			using (var profiler = Profile())
			using (var connection = _fixture.GetConnection())
			{
				// arange
				connection.Open();
				connection.Truncate<Computed>();
				var records = TestData.ComputedData(10);

				var recordsToInsert = records.Take(9);
				var inserted = connection.InsertBulkAsync(recordsToInsert, outputMap: OutputMapper.MapOnce).Result;

				foreach (var record in recordsToInsert.Skip(1))
				{
					record.Name = (record.Id + 2).ToString();
					record.Value = record.Value + 2;
					record.ValueDate = record.ValueDate.AddDays(30);
				}

				// act
				var updated = connection.UpsertBulkAsync(records.Skip(1), outputMap: OutputMapper.Map).Result;

				// assert
				var updatedRecords = connection.GetAll<Computed>();
				Assert.Equal(9, updated);
				Assert.Equal("1", updatedRecords.First(x => x.Id == 1).Name);
				for (var i = 0; i < records.Count(); i++)
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
