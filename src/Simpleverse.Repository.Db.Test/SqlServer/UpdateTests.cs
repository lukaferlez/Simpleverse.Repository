using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
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
	public class UpdateTests : DatabaseTestFixture
	{
		public UpdateTests(DatabaseFixture fixture, ITestOutputHelper output)
			: base(fixture, output)
		{
		}

		[Theory]
		[ClassData(typeof(UpdateTestData))]
		public void UpdateTest<T, TKey>(string testName, IEnumerable<T> records, Func<T, TKey> keySelector, Action<T, T> check, int expected) where T : Identity
		{
			using (var profiler = Profile(testName))
			using (var connection = _fixture.GetProfiledConnection())
			{
				Arange<T>(connection);

				var inserted = connection.Insert(records);
				records = records.Skip(1);
				foreach (var record in records)
					record.Name = (record.Id + 2).ToString();

				// act
				var updated = connection.UpdateBulkAsync(records, outputMap: OutputMapper.Map).Result;

				// assert
				Assert(connection, records, check, records.Count(), updated);
			}
		}

		[Theory]
		[ClassData(typeof(UpdateTestData))]
		public void UpdateTransactionAsyncTest<T, TKey>(string testName, IEnumerable<T> records, Func<T, TKey> keySelector, Action<T, T> check, int expected) where T : Identity
		{
			using (var profiler = Profile(testName))
			using (var connection = _fixture.GetProfiledConnection())
			{
				Arange<T>(connection);

				var inserted = connection.Insert(records);
				records = records.Skip(1);
				foreach (var record in records)
					record.Name = (record.Id + 2).ToString();

				// act
				var updated = 0;
				using (var transaction = connection.BeginTransaction())
				{
					updated = connection.UpdateBulkAsync(records, outputMap: OutputMapper.Map, transaction: transaction).Result;
					transaction.Commit();
				}

				Assert<T>(connection, records, check, records.Count(), updated);
			}
		}

		[Theory]
		[ClassData(typeof(UpdateDuplicateTestData))]
		public void UpdateDuplicateTest<T, TKey>(string testName, IEnumerable<T> records, Func<T, TKey> keySelector, Action<T, T> check, int expected) where T : Identity
		{
			using (var profiler = Profile(testName))
			using (var connection = _fixture.GetProfiledConnection())
			{
				Arange<T>(connection);

				var inserted = connection.Insert(records);
				var firstRecordId = records.First().Id;
				records = records.Where(x => x.Id != firstRecordId).ToList();
				foreach (var record in records)
					record.Name = (record.Id + 2).ToString();

				// act
				var updated = connection.UpdateBulkAsync(records, outputMap: OutputMapper.Map).Result;

				// assert
				Assert(connection, records, check, records.Count() / 2, updated);
			}
		}

		[Theory]
		[ClassData(typeof(UpdateImmutableTestData))]
		public async Task UpdateImmutableTest<T, TKey>(string testName, IEnumerable<T> records, Func<T, TKey> keySelector, Action<T, T> check, int expected) where T : Immutable
		{
			using (var profiler = Profile(testName))
			using (var connection = _fixture.GetProfiledConnection())
			{
				Arange<T>(connection);

				var inserted = await connection.InsertAsync(records);
				foreach (var record in records)
					record.ImmutableValue = 100;

				// act
				var updated = await connection.UpdateBulkAsync(records, outputMap: OutputMapper.Map);

				// assert
				Assert(connection, records, check, records.Count(), updated);
			}
		}

		private void Arange<T>(IDbConnection connection) where T : class
		{
			connection.Open();
			_fixture.TearDownDb();
			_fixture.SetupDb();
		}

		private void Assert<T>(IDbConnection connection, IEnumerable<T> records, Action<T, T> check, int expected, int updated) where T : Identity
		{
			IEnumerable<T> updatedRecords = null;

			// Workaround for Dapper.Contrib not supporting composite keys
			if (typeof(T).Equals(typeof(IdentityAndExplict)))
				updatedRecords = connection.Query<T>("SELECT * FROM [IdentityAndExplict]");
			else
				updatedRecords = connection.GetAll<T>();

			Xunit.Assert.Equal(expected, updated);
			Xunit.Assert.Equal("1", updatedRecords.First(x => x.Id == 1).Name);
			for (var i = 0; i < records.Count(); i++)
			{
				var record = records.ElementAt(i);
				var updatedRecord = updatedRecords.FirstOrDefault(x => x.Id == record.Id);

				check(record, updatedRecord);
			}
		}
	}

	public class UpdateTestData : IEnumerable<object[]>
	{
		public IEnumerator<object[]> GetEnumerator()
		{
			foreach (var data in DataSet(2))
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
			yield return InsertTestData.IdentityTestWithId(count);
			yield return InsertTestData.IdentityAndExplictTest(count);
			yield return InsertTestData.ComputedTest(count);
			yield return InsertTestData.WriteTest(count);
			yield return InsertTestData.DataTypeTest(count);
			yield return InsertTestData.DataTypeNullableTest(count);
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}

	public class UpdateDuplicateTestData : IEnumerable<object[]>
	{
		public IEnumerator<object[]> GetEnumerator()
		{
			foreach (var data in DataSet(2))
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
			yield return InsertTestData.ComputedDuplicateTest(count);
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}

	public class UpdateImmutableTestData : IEnumerable<object[]>
	{
		public IEnumerator<object[]> GetEnumerator()
		{
			foreach (var data in DataSet(2))
				yield return data;

			foreach (var data in DataSet(10))
				yield return data;

			foreach (var data in DataSet(500))
				yield return data;
		}

		public IEnumerable<object[]> DataSet(int count)
		{
			yield return InsertTestData.ImmutableTest(count);
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
