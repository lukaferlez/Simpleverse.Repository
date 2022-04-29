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
	public class InsertTests : IClassFixture<DatabaseFixture>
	{
		DatabaseFixture fixture;

		public InsertTests(DatabaseFixture fixture)
		{
			this.fixture = fixture;
		}

		[Theory]
		[ClassData(typeof(InsertTestData))]
		public void InsertAsyncTest<T>(string testName, IEnumerable<T> records, Action<T, T> check, int expected) where T: class
		{
			using (var connection = fixture.GetConnection())
			{
				Arange<T>(connection);

				var inserted = connection.InsertBulkAsync(records).Result;

				Assert<T>(connection, records, check, expected, inserted);
			}
		}

		[Theory]
		[ClassData(typeof(InsertTestData))]
		public void InsertTransactionAsyncTest<T>(string testName, IEnumerable<T> records, Action<T, T> check, int expected) where T: class
		{
			using (var connection = fixture.GetConnection())
			{
				Arange<T>(connection);

				int inserted = 0;
				using (var transaction = connection.BeginTransaction())
				{
					inserted = connection.InsertBulkAsync(records, transaction: transaction).Result;
					transaction.Commit();
				}

				Assert<T>(connection, records, check, expected, inserted);
			}
		}

		private void Arange<T>(IDbConnection connection) where T : class
		{
			connection.Open();
			connection.DeleteAll<T>();
		}

		private void Assert<T>(IDbConnection connection, IEnumerable<T> records, Action<T, T> check, int expected, int inserted) where T : class
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
			for (int i = 0; i < records.Count(); i++)
			{
				var record = records.ElementAt(i);
				var insertedRecord = insertedRecords.ElementAt(i);

				check(record, insertedRecord);
			}
		}
	}

	public class InsertTestData : IEnumerable<object[]>
	{
		private static object[] Generate<T>(string name, Func<int, IEnumerable<T>> generator, Action<T, T> check, int count)
		{
			return new object[] { TestName(name, count), generator(count), check, count };
		}

		private static string TestName(string name, int itemCount) => $"{name}-{itemCount}";

		public static object[] TableEscapeTest(int count) =>
			Generate(nameof(TableEscapeTest), TestData.TableEscapeData, (record, inserted) => { Assert.Equal(record.NoId, record.NoId); }, count);

		public static object[] TableEscapeWithSchemaTest(int count) =>
			Generate(
				nameof(TableEscapeWithSchemaTest),
				TestData.TableEscapeWithSchemaData,
				(record, inserted) => { Assert.Equal(record.NoId, record.NoId); },
				count
			);

		public static object[] IdentityTest(int count) =>
			Generate(
				nameof(IdentityTest),
				TestData.IdentityData,
				(record, inserted) => { Assert.Equal(record.Name, inserted.Name); },
				count
			);

		public static object[] ExplicitKeyTest(int count) =>
			Generate(
				nameof(ExplicitKeyTest),
				TestData.ExplicitKeyData,
				(record, inserted) => {
					Assert.Equal(record.Id, inserted.Id);
					Assert.Equal(record.Name, inserted.Name);
				},
				count
			);

		public static object[] IdentityAndExplictTest(int count) =>
			Generate(
				nameof(IdentityAndExplictTest),
				TestData.IdentityAndExplictData,
				(record, inserted) => {
					Assert.Equal(record.ExplicitKeyId, inserted.ExplicitKeyId);
					Assert.Equal(record.Name, inserted.Name);
				},
				count
			);

		public static object[] ComputedTest(int count) =>
			Generate(
				nameof(ComputedTest),
				TestData.ComputedData,
				(record, inserted) => {
					Assert.Equal(record.Name, inserted.Name);
					Assert.Equal(5, inserted.Value);
				},
				count
			);

		public static object[] WriteTest(int count) =>
			Generate(
				nameof(WriteTest),
				TestData.WriteData,
				(record, inserted) => {
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
				(record, inserted) => {
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
				(record, inserted) => {
					Assert.Equal(record.Name, inserted.Name);
					Assert.Equal(record.Enum, inserted.Enum);
					Assert.Equal(record.Guid, inserted.Guid);
					Assert.Equal(record.DateTime, inserted.DateTime);
				},
				count
			);

		public IEnumerator<object[]> GetEnumerator()
		{
			foreach(var data in DataSet(0))
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
			yield return IdentityTest(count);
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
