using Dapper.Contrib.Extensions;
using Simpleverse.Repository.Db.Extensions;
using Simpleverse.Repository.Db.SqlServer;
using Simpleverse.Repository.Db.SqlServer.Merge;
using System;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Simpleverse.Repository.Db.Test.SqlServer.Merge
{
	[Collection("SqlServerCollection")]
	public class UpsertTests : DatabaseTestFixture
	{
		public UpsertTests(DatabaseFixture fixture, ITestOutputHelper output)
			: base(fixture, output)
		{
		}

		[Fact]
		public void UpsertAsyncExplicitKeyTest()
		{
			using (var profiler = Profile())
			using (var connection = _fixture.GetProfiledConnection())
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
			using (var connection = _fixture.GetProfiledConnection())
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
			using (var connection = _fixture.GetProfiledConnection())
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
			using (var connection = _fixture.GetProfiledConnection())
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
			using (var connection = _fixture.GetProfiledConnection())
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
			using (var connection = _fixture.GetProfiledConnection())
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
			using (var connection = _fixture.GetProfiledConnection())
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
		public void UpsertBulkAsyncMapsInsertedAndUpdatedTest()
		{
			using (var profiler = Profile())
			using (var connection = _fixture.GetProfiledConnection())
			{
				// arange
				connection.Open();
				connection.Truncate<Computed>();

				var existing = TestData.ComputedData(5).ToList();
				connection.InsertBulkAsync(existing, outputMap: OutputMapper.MapOnce).Wait();

				foreach (var record in existing)
				{
					record.Name = record.Name + "-updated";
					record.Value = 0;
					record.ValueDate = default;
					record.ValueComputed = 0;
				}

				var added = TestData.ComputedData(3).ToList();
				foreach (var record in added)
				{
					record.Id = 0;
					record.Name = "new-" + record.Name;
				}

				var records = existing.Concat(added).ToList();

				// act
				var affected = connection.UpsertBulkAsync(records, outputMap: OutputMapper.Map).Result;

				// assert
				Assert.Equal(8, affected);
				Assert.All(records, x => Assert.NotEqual(0, x.Id));
				Assert.All(records, x => Assert.Equal(5, x.Value));
				Assert.All(records, x => Assert.Equal(10, x.ValueComputed));
				Assert.All(records, x => Assert.Equal(new DateTime(2022, 05, 02), x.ValueDate));
			}
		}

		[Fact]
		public void UpsertBulkAsyncSkipsUnchangedMatchedRecordsByDefaultTest()
		{
			using (var profiler = Profile())
			using (var connection = _fixture.GetProfiledConnection())
			{
				// arange
				connection.Open();
				connection.Truncate<Computed>();

				var existing = TestData.ComputedData(5).ToList();
				connection.InsertBulkAsync(existing, outputMap: OutputMapper.MapOnce).Wait();

				// nothing changed, only the generated values are cleared locally
				foreach (var record in existing)
				{
					record.Value = 0;
					record.ValueDate = default;
					record.ValueComputed = 0;
				}

				// act
				var affected = connection.UpsertBulkAsync(existing, outputMap: OutputMapper.Map).Result;

				// assert
				// by default unchanged entities are not written, so they produce no output row to map from
				Assert.Equal(0, affected);
				Assert.All(existing, x => Assert.Equal(0, x.ValueComputed));
			}
		}

		[Fact]
		public void UpsertBulkAsyncWithoutConditionCheckMapsUnchangedMatchedRecordsTest()
		{
			using (var profiler = Profile())
			using (var connection = _fixture.GetProfiledConnection())
			{
				// arange
				connection.Open();
				connection.Truncate<Computed>();

				var existing = TestData.ComputedData(5).ToList();
				connection.InsertBulkAsync(existing, outputMap: OutputMapper.MapOnce).Wait();

				// nothing changed, only the generated values are cleared locally
				foreach (var record in existing)
				{
					record.Value = 0;
					record.ValueDate = default;
					record.ValueComputed = 0;
				}

				// act
				var affected = connection.UpsertBulkAsync(
					existing,
					outputOptions: options =>
					{
						options.Map = OutputMapper.Map;
						options.MapChangedOnly = false;
					}
				).Result;

				// assert
				Assert.Equal(5, affected);
				Assert.All(existing, x => Assert.Equal(5, x.Value));
				Assert.All(existing, x => Assert.Equal(10, x.ValueComputed));
				Assert.All(existing, x => Assert.Equal(new DateTime(2022, 05, 02), x.ValueDate));
			}
		}

		[Fact]
		public void UpsertBulkAsyncWithoutConditionCheckMapsUnchangedMatchedRecordsOnCustomKeyTest()
		{
			using (var profiler = Profile())
			using (var connection = _fixture.GetProfiledConnection())
			{
				// arange
				connection.Open();
				connection.Truncate<Identity>();

				var existing = TestData.IdentityWithoutIdData(3).ToList();
				connection.InsertBulkAsync(existing, outputMap: OutputMapper.MapOnce).Wait();

				// caller only knows the business key and changes nothing
				var records = TestData.IdentityWithoutIdData(3).ToList();

				// act
				var affected = connection.UpsertBulkAsync(
					records,
					key: options => options.ColumnsByName(nameof(Identity.Name)),
					outputOptions: options =>
					{
						options.Map = OutputMapper.Map;
						options.MapChangedOnly = false;
					}
				).Result;

				// assert
				Assert.Equal(3, affected);
				Assert.All(records, x => Assert.NotEqual(0, x.Id));
			}
		}

		[Fact]
		public void UpsertBulkAsyncMapsUpdatedRecordsMatchedOnCustomKeyTest()
		{
			using (var profiler = Profile())
			using (var connection = _fixture.GetProfiledConnection())
			{
				// arange
				connection.Open();
				connection.Truncate<Identity>();

				var existing = TestData.IdentityWithoutIdData(3).ToList();
				connection.InsertBulkAsync(existing, outputMap: OutputMapper.MapOnce).Wait();

				// caller only knows the business key, not the identity
				var records = TestData.IdentityWithoutIdData(5).ToList();
				foreach (var record in records)
					record.From = "changed";

				// act
				connection.UpsertBulkAsync(
					records,
					key: options => options.ColumnsByName(nameof(Identity.Name)),
					outputMap: OutputMapper.Map
				).Wait();

				// assert
				Assert.All(records, x => Assert.NotEqual(0, x.Id));
			}
		}

		[Fact]
		public void UpsertBulkAsyncWriteAttributeTest()
		{
			using (var profiler = Profile())
			using (var connection = _fixture.GetProfiledConnection())
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
			using (var connection = _fixture.GetProfiledConnection())
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
			using (var connection = _fixture.GetProfiledConnection())
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
