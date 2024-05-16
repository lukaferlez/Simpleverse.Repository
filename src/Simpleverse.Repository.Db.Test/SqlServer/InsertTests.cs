using Dapper;
using Dapper.Contrib.Extensions;
using MoreLinq;
using Simpleverse.Repository.Db.SqlServer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Simpleverse.Repository.Db.Test.SqlServer
{
	[Collection("SqlServerCollection")]
	public class InsertTests : DatabaseTestFixture
	{
		public InsertTests(DatabaseFixture fixture, ITestOutputHelper output)
			: base(fixture, output)
		{
		}

		[Theory]
		[ClassData(typeof(InsertTestData))]
		public void InsertAsyncTest<T, TKey>(string testName, IEnumerable<T> records, Func<T, TKey> keySelector, Action<T, T> check, int expected) where T : class
		{
			using (var profiler = Profile(testName))
			using (var connection = _fixture.GetProfiledConnection())
			{
				Arange<T>(connection);

				var inserted = connection.InsertBulkAsync(records, outputMap: OutputMapper.MapOnce).Result;

				Assert(connection, records, keySelector, check, expected, inserted);
			}
		}

		[Theory]
		[ClassData(typeof(InsertTestData))]
		public void InsertTransactionAsyncTest<T, TKey>(string testName, IEnumerable<T> records, Func<T, TKey> keySelector, Action<T, T> check, int expected) where T : class
		{
			using (var profiler = Profile(testName))
			using (var connection = _fixture.GetProfiledConnection())
			{
				Arange<T>(connection);

				var inserted = 0;
				using (var transaction = connection.BeginTransaction())
				{
					inserted = connection.InsertBulkAsync(records, transaction: transaction, outputMap: OutputMapper.MapOnce).Result;
					transaction.Commit();
				}

				Assert(connection, records, keySelector, check, expected, inserted);
			}
		}

		[Theory]
		[ClassData(typeof(InsertTestData))]
		public async Task InsertParallelAsyncTest<T, TKey>(string testName, IEnumerable<T> records, Func<T, TKey> keySelector, Action<T, T> check, int expected) where T : class
		{
			using (var connection = _fixture.GetConnection())
			{
				// arrange	
				Arange<T>(connection);
				var recordCount = records.Count();
				var records1 = records.Take(recordCount / 2).ToList();
				var records2 = records.Skip(recordCount / 2).ToList();

				// act
				var insertTask1 = Execute(testName + "_batch1", records1);
				var insertTask2 = Execute(testName + "_batch2", records2);
				var inserted1 = await insertTask1;
				var inserted2 = await insertTask2;

				// assert
				Assert(connection, records, keySelector, check, expected, inserted1 + inserted2);
			}

			async Task<int> Execute(string runName, List<T> records)
			{
				using (var profiler = Profile(runName))
				using (var connection = _fixture.GetProfiledConnection())
				{
					return await connection.InsertBulkAsync(records, outputMap: OutputMapper.MapOnce);
				}
			}
		}

		private void Arange<T>(IDbConnection connection) where T : class
		{
			connection.Open();
			connection.DeleteAll<T>();
		}

		private void Assert<T, TKey>(
			IDbConnection connection,
			IEnumerable<T> records,
			Func<T, TKey> keySelector,
			Action<T, T> check,
			int expected,
			int inserted
		) where T : class
		{
			IEnumerable<T> insertedRecords = null;

			// Workaround for Dapper.Contrib not supporting composite keys
			if (typeof(T).Equals(typeof(IdentityAndExplict)))
				insertedRecords = connection.Query<T>("SELECT * FROM [IdentityAndExplict]");
			else
				insertedRecords = connection.GetAll<T>();

			Xunit.Assert.Equal(expected, inserted);
			Xunit.Assert.Equal(records.Count(), inserted);
			Xunit.Assert.Equal(records.Count(), insertedRecords.Count());

			var joinedResults =
				records
					.Join(
						insertedRecords,
						keySelector,
						keySelector,
						(record, inserted) => new { record, inserted }
					);

			Xunit.Assert.Equal(records.Count(), joinedResults.Count());

			joinedResults
				.ForEach(
					x => check(x.record, x.inserted)
				);
		}
	}

	public class InsertTestData : IEnumerable<object[]>
	{
		public static object[] Generate<T, TKey>(string name, Func<int, IEnumerable<T>> generator, Func<T, TKey> keySelector, Action<T, T> check, int count)
		{
			return new object[] { TestName(name, count), generator(count), keySelector, check, count };
		}

		public static object[] GenerateDuplicate<T, TKey>(string name, Func<int, IEnumerable<T>> generator, Func<T, TKey> keySelector, Action<T, T> check, int count)
		{
			return new object[] { TestName(name, count) + "duplicate", generator(count).Union(generator(count)), keySelector, check, count * 2 };
		}

		private static string TestName(string name, int itemCount) => $"{name}-{itemCount}";

		public static object[] TableEscapeTest(int count) =>
			Generate(nameof(TableEscapeTest), TestData.TableEscapeData, x => x.NoId, (record, inserted) => { Assert.Equal(record.NoId, record.NoId); }, count);

		public static object[] TableEscapeWithSchemaTest(int count) =>
			Generate(
				nameof(TableEscapeWithSchemaTest),
				TestData.TableEscapeWithSchemaData,
				x => x.NoId,
				(record, inserted) => { Assert.Equal(record.NoId, record.NoId); },
				count
			);

		public static object[] IdentityTestWithoutId(int count) =>
			Generate(
				nameof(IdentityTestWithoutId),
				TestData.IdentityWithoutIdData,
				x => x.Id,
				(record, inserted) =>
				{
					Assert.Equal(inserted.Id, record.Id);
					Assert.Equal(record.Name, inserted.Name);
				},
				count
			);

		public static object[] IdentityTestWithoutIdDuplicate(int count) =>
			GenerateDuplicate(
				nameof(IdentityTestWithoutId),
				TestData.IdentityWithoutIdData,
				x => x.Id,
				(record, inserted) =>
				{
					Assert.NotEqual(0, record.Id);
					Assert.Equal(record.Name, inserted.Name);
				},
				count
			);

		public static object[] IdentityTestWithId(int count) =>
			Generate(
				nameof(IdentityTestWithId),
				TestData.IdentityWithIdData,
				x => x.Id,
				(record, inserted) =>
				{
					Assert.Equal(inserted.Id, record.Id);
					Assert.Equal(record.Name, inserted.Name);
				},
				count
			);

		public static object[] ExplicitKeyTest(int count) =>
			Generate(
				nameof(ExplicitKeyTest),
				TestData.ExplicitKeyData,
				x => x.Id,
				(record, inserted) =>
				{
					Assert.Equal(record.Id, inserted.Id);
					Assert.Equal(record.Name, inserted.Name);
				},
				count
			);

		public static object[] IdentityAndExplictTest(int count) =>
			Generate(
				nameof(IdentityAndExplictTest),
				TestData.IdentityAndExplictData,
				x => new { x.Id, x.ExplicitKeyId },
				(record, inserted) =>
				{
					Assert.Equal(record.ExplicitKeyId, inserted.ExplicitKeyId);
					Assert.Equal(record.Name, inserted.Name);
				},
				count
			);

		public static object[] ComputedTest(int count) =>
			Generate(
				nameof(ComputedTest),
				TestData.ComputedData,
				x => x.Id,
				(record, inserted) =>
				{
					Assert.Equal(record.Name, inserted.Name);
					Assert.Equal(inserted.Value, record.Value);
					Assert.Equal(inserted.ValueDate, record.ValueDate);
					Assert.Equal(inserted.Value * 2, record.ValueComputed);
				},
				count
			);

		public static object[] ComputedDuplicateTest(int count) =>
			GenerateDuplicate(
				nameof(ComputedTest),
				TestData.ComputedData,
				x => x.Id,
				(record, inserted) =>
				{
					Assert.Equal(record.Name, inserted.Name);
					Assert.Equal(inserted.Value, record.Value);
					Assert.Equal(inserted.ValueDate, record.ValueDate);
					Assert.Equal(inserted.Value * 2, record.ValueComputed);
				},
				count
			);

		public static object[] WriteTest(int count) =>
			Generate(
				nameof(WriteTest),
				TestData.WriteData,
				x => x.Id,
				(record, inserted) =>
				{
					Assert.Equal(record.Name, inserted.Name);
					Assert.Null(inserted.Ignored);
					Assert.Equal(record.NotIgnored, inserted.NotIgnored);
				},
				count
			);

		public static object[] DataTypeTest(int count) =>
			Generate(
				nameof(DataTypeTest),
				TestData.DataTypeData,
				x => x.Id,
				(record, inserted) =>
				{
					Assert.Equal(record.Name, inserted.Name);
					Assert.Equal(record.Enum, inserted.Enum);
					Assert.Equal(record.Guid, inserted.Guid);
					Assert.Equal(record.DateTime, inserted.DateTime);
				},
				count
			);

		public static object[] DataTypeNullableTest(int count) =>
			Generate(
				nameof(DataTypeNullableTest),
				TestData.DataTypeNullableData,
				x => x.Id,
				(record, inserted) =>
				{
					Assert.Equal(record.Name, inserted.Name);
					Assert.Equal(record.Enum, inserted.Enum);
					Assert.Equal(record.Guid, inserted.Guid);
					Assert.Equal(record.DateTime, inserted.DateTime);
				},
				count
			);

		public IEnumerator<object[]> GetEnumerator()
		{
			foreach (var data in DataSet(0))
				yield return data;

			foreach (var data in DataSet(1))
				yield return data;

			foreach (var data in DataSet(10))
				yield return data;

			foreach (var data in DataSet(500))
				yield return data;

			//foreach (var data in DataSet(3000))
			//	yield return data;

			//foreach (var data in DataSet(20010))
			//	yield return data;
		}

		public IEnumerable<object[]> DataSet(int count)
		{
			yield return TableEscapeTest(count);
			yield return TableEscapeWithSchemaTest(count);
			yield return IdentityTestWithoutId(count);
			yield return IdentityTestWithoutIdDuplicate(count);
			yield return ExplicitKeyTest(count);
			yield return IdentityAndExplictTest(count);
			yield return ComputedTest(count);
			yield return WriteTest(count);
			yield return DataTypeTest(count);
			yield return DataTypeNullableTest(count);
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
