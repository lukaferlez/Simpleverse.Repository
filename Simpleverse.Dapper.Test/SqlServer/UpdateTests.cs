using System;
using System.Collections.Generic;
using Xunit;
using Dapper.Contrib.Extensions;
using System.Linq;
using System.Data;
using System.Collections;
using Simpleverse.Dapper.SqlServer;
using Dapper;

namespace Simpleverse.Dapper.Test.SqlServer
{
	[Collection("SqlServerCollection")]
	public class UpdateTests : IClassFixture<DatabaseFixture>
	{
		DatabaseFixture fixture;

		public UpdateTests(DatabaseFixture fixture)
		{
			this.fixture = fixture;
		}

		[Theory]
		[ClassData(typeof(UpdateTestData))]
		public void UpdateTest<T>(string testName, IEnumerable<T> records, Action<T, T> check, int expected) where T : Identity
		{
			using (var connection = fixture.GetConnection())
			{
				Arange<T>(connection);

				var inserted = connection.Insert(records);
				records = records.Skip(1);
				foreach (var record in records)
					record.Name = (record.Id + 2).ToString();

				// act
				var updated = connection.UpdateBulkAsync(records).Result;

				// assert
				Assert(connection, records, check, records.Count(), updated);
			}
		}

		[Theory]
		[ClassData(typeof(UpdateTestData))]
		public void UpdateTransactionAsyncTest<T>(string testName, IEnumerable<T> records, Action<T, T> check, int expected) where T : Identity
		{
			using (var connection = fixture.GetConnection())
			{
				Arange<T>(connection);

				var inserted = connection.Insert(records);
				records = records.Skip(1);
				foreach (var record in records)
					record.Name = (record.Id + 2).ToString();

				// act
				int updated = 0;
				using (var transaction = connection.BeginTransaction())
				{
					updated = connection.UpdateBulkAsync(records, transaction: transaction).Result;
					transaction.Commit();
				}

				Assert<T>(connection, records, check, records.Count(), updated);
			}
		}

		private void Arange<T>(IDbConnection connection) where T : class
		{
			connection.Open();
			fixture.TearDownDb();
			fixture.SetupDb();
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
			for (int i = 0; i < records.Count(); i++)
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
			yield return InsertTestData.IdentityTest(2);
			yield return InsertTestData.IdentityTest(10);
			yield return InsertTestData.IdentityAndExplictTest(2);
			yield return InsertTestData.IdentityAndExplictTest(10);
			yield return InsertTestData.ComputedTest(2);
			yield return InsertTestData.ComputedTest(10);
			yield return InsertTestData.WriteTest(2);
			yield return InsertTestData.WriteTest(10);
			yield return InsertTestData.DataTypeTest(2);
			yield return InsertTestData.DataTypeTest(10);
			yield return InsertTestData.DataTypeNullableTest(2);
			yield return InsertTestData.DataTypeNullableTest(10);
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
