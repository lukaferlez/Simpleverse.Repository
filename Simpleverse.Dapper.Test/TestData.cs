using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;

namespace Simpleverse.Dapper.Test
{
	public class TestData
	{
		private static IEnumerable<T> Generate<T>(Func<int, T> generator, int count)
		{
			var items = new List<T>();
			for (int i = 0; i < count; i++)
				items.Add(generator(i + 1));

			return items;
		}

		public static IEnumerable<TableEscape> TableEscapeData(int count) =>
			Generate(x => new TableEscape() { NoId = x }, count);

		public static IEnumerable<TableEscapeWithSchema> TableEscapeWithSchemaData(int count) =>
			Generate(x => new TableEscapeWithSchema() { NoId = x }, count);

		public static IEnumerable<Identity> IdentityData(int count) =>
			Generate(x => new Identity() { Id = x, Name = x.ToString() }, count);

		public static IEnumerable<ExplicitKey> ExplicitKeyData(int count) =>
			Generate(x => new ExplicitKey() { Id = x, Name = x.ToString() }, count);

		public static IEnumerable<IdentityAndExplict> IdentityAndExplictData(int count) =>
			Generate(
				x => new IdentityAndExplict() {
					Id = x,
					ExplicitKeyId = Guid.NewGuid(),
					Name = x.ToString()
				},
				count
			);

		public static IEnumerable<Computed> ComputedData(int count) =>
			Generate(
				x => new Computed() {
					Id = x,
					Name = x.ToString()
				},
				count
			);

		public static IEnumerable<Write> WriteData(int count) =>
			Generate(
				x => new Write() {
					Id = x,
					Name = x.ToString(),
					Ignored = 50,
					NotIgnored = 100
				},
				count
			);

		public static IEnumerable<DataType> DataTypeData(int count) =>
			Generate(
				x => new DataType() {
					Id = x,
					Name = x.ToString(),
					Enum = x % 2 == 0 ? Enum.Even : Enum.Odd,
					Guid = Guid.NewGuid(),
					DateTime = DateTime.Today
				},
				count
			);

		public static IEnumerable<DataTypeNullable> DataTypeNullableData(int count) =>
			Generate(
				x => new DataTypeNullable() {
					Id = x,
					Name = x.ToString(),
					Enum = x % 2 == 0 ? (@Enum?)null : Enum.Odd,
					Guid = x % 2 == 0 ? (Guid?)null : Guid.NewGuid(),
					DateTime = x % 2 == 0 ? (DateTime?)null : DateTime.Today,
				},
				count
			);
	}

	[Table("[10_Escape]")]
	public class TableEscape
	{
		[ExplicitKey]
		public int NoId { get; set; }
	}

	[Table("Test.[10_Escape]")]
	public class TableEscapeWithSchema : TableEscape
	{
	}

	[Table("[Identity]")]
	public class Identity
	{
		[Key]
		public int Id { get; set; }

		public string Name { get; set; }
	}

	[Table("[ExplicitKey]")]
	public class ExplicitKey
	{
		[ExplicitKey]
		public int Id { get; set; }

		public string Name { get; set; }
	}

	[Table("[IdentityAndExplict]")]
	public class IdentityAndExplict : Identity
	{
		[ExplicitKey]
		public Guid ExplicitKeyId { get; set; }
	}

	[Table("[Computed]")]
	public class Computed : Identity
	{
		[Computed]
		public int Value { get; set; }
		[Computed]
		public DateTime ValueDate { get; set; }
	}

	[Table("[Write]")]
	public class Write : Identity
	{
		[Write(false)]
		public int? Ignored { get; set; }

		[Write(true)]
		public int? NotIgnored { get; set; }
	}

	public enum @Enum
	{
		Odd = 1,
		Even = 2
	}

	[Table("[DataType]")]
	public class DataType : Identity
	{
		public @Enum Enum { get; set; }

		public Guid Guid { get; set; }

		public DateTime DateTime { get; set; }
	}

	[Table("DataType")]
	public class DataTypeNullable : Identity
	{
		public @Enum? Enum { get; set; }

		public Guid? Guid { get; set; }

		public DateTime? DateTime { get; set; }
	}
}
