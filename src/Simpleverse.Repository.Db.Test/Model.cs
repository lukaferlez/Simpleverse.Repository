using System;

namespace Simpleverse.Repository.Db.Test
{
	public class Model
	{
		public int Int { get; set; }
		public int? IntNullable { get; set; }
		public @Enum EnumValue { get; set; }
		public Enum? EnumValueNullable { get; set; }
		public DateTime DateTime { get; set; }
		public DateTime? DateTimeNullable { get; set; }
		public string String { get; set; }
		public bool Bool { get; set; }
		public bool? BoolNullable { get; set; }
		public Guid Guid { get; set; }
		public Guid? GuidNullable { get; set; }
	}
}
