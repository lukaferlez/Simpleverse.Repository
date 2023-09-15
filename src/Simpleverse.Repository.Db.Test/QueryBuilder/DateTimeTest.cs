using System;
using Xunit;

namespace Simpleverse.Repository.Db.Test.QueryBuilder
{
	public class DateTimeTest : QueryBuilderTestFixture
	{
		public DateTime Value { get; } = new DateTime(2023, 7, 7);
		public DateTime? ValueNullable { get; } = new DateTime(2023, 7, 7);
		public DateTime? ValueNull { get; }

		[Fact]
		public void TestBetween_Nullable_With_FromNonNullableValue()
			=> Test<Model>(
				queryBuilder => queryBuilder.WhereBetween(x => x.DateTimeNullable, Value, null),
				"WHERE [DateTimeNullable] >= @DateTimeNullableFrom",
				new[] { "DateTimeNullableFrom" }
			);

		[Fact]
		public void TestBetween_Nullable_With_ToNonNullableValue()
			=> Test<Model>(
				queryBuilder => queryBuilder.WhereBetween(x => x.DateTimeNullable, null, Value),
				"WHERE [DateTimeNullable] <= @DateTimeNullableTo",
				new[] { "DateTimeNullableTo" }
			);

		[Fact]
		public void TestBetween_Nullable_With_NonNullableValue()
			=> Test<Model>(
				queryBuilder => queryBuilder.WhereBetween(x => x.DateTimeNullable, Value, Value),
				"WHERE [DateTimeNullable] >= @DateTimeNullableFrom AND [DateTimeNullable] <= @DateTimeNullableTo",
				new[] { "DateTimeNullableFrom", "DateTimeNullableTo" }
			);

		[Fact]
		public void TestBetween_NonNullable_With_FromNonNullableValue()
			=> Test<Model>(
				queryBuilder => queryBuilder.WhereBetween(x => x.DateTime, Value, null),
				"WHERE [DateTime] >= @DateTimeFrom",
				new[] { "DateTimeFrom" }
			);

		[Fact]
		public void TestBetween_NonNullable_With_ToNonNullableValue()
			=> Test<Model>(
				queryBuilder => queryBuilder.WhereBetween(x => x.DateTime, null, Value),
				"WHERE [DateTime] <= @DateTimeTo",
				new[] { "DateTimeTo" }
			);

		[Fact]
		public void TestBetween_NonNullable_With_NonNullableValue()
			=> Test<Model>(
				queryBuilder => queryBuilder.WhereBetween(x => x.DateTime, Value, Value),
				"WHERE [DateTime] >= @DateTimeFrom AND [DateTime] <= @DateTimeTo",
				new[] { "DateTimeFrom", "DateTimeTo" }
			);

		[Fact]
		public void TestBetween_Nullable_With_FromNullableValue()
			=> Test<Model>(
				queryBuilder => queryBuilder.WhereBetween(x => x.DateTimeNullable, ValueNullable, null),
				"WHERE [DateTimeNullable] >= @DateTimeNullableFrom",
				new[] { "DateTimeNullableFrom" }
			);

		[Fact]
		public void TestBetween_Nullable_With_ToNullableValue()
			=> Test<Model>(
				queryBuilder => queryBuilder.WhereBetween(x => x.DateTimeNullable, null, ValueNullable),
				"WHERE [DateTimeNullable] <= @DateTimeNullableTo",
				new[] { "DateTimeNullableTo" }
			);

		[Fact]
		public void TestBetween_Nullable_With_NullableValue()
			=> Test<Model>(
				queryBuilder => queryBuilder.WhereBetween(x => x.DateTimeNullable, ValueNullable, ValueNullable),
				"WHERE [DateTimeNullable] >= @DateTimeNullableFrom AND [DateTimeNullable] <= @DateTimeNullableTo",
				new[] { "DateTimeNullableFrom", "DateTimeNullableTo" }
			);

		[Fact]
		public void TestBetween_NonNullable_With_FromNullableValue()
			=> Test<Model>(
				queryBuilder => queryBuilder.WhereBetween(x => x.DateTime, ValueNullable, null),
				"WHERE [DateTime] >= @DateTimeFrom",
				new[] { "DateTimeFrom" }
			);

		[Fact]
		public void TestBetween_NonNullable_With_ToNullableValue()
			=> Test<Model>(
				queryBuilder => queryBuilder.WhereBetween(x => x.DateTime, null, ValueNullable),
				"WHERE [DateTime] <= @DateTimeTo",
				new[] { "DateTimeTo" }
			);

		[Fact]
		public void TestBetween_NonNullable_With_NullableValue()
			=> Test<Model>(
				queryBuilder => queryBuilder.WhereBetween(x => x.DateTime, ValueNullable, ValueNullable),
				"WHERE [DateTime] >= @DateTimeFrom AND [DateTime] <= @DateTimeTo",
				new[] { "DateTimeFrom", "DateTimeTo" }
			);
	}
}
