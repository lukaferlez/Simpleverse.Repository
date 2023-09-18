using System.Linq;
using Xunit;
using Simpleverse.Repository.Db.Extensions.Dapper;
using Simpleverse.Repository.Db.Test.QueryBuilder;

namespace Simpleverse.Repository.Db.Test.Extensions.Dapper
{
	public class WhereTest : WhereTestFixture
	{
		[Fact]
		public void WhereFlagsTest()
			=> Test(
				builder =>
				{
					builder.Where<int>("Field", 1, "D");
					builder.Where<EnumFlags>("Field2", EnumFlags.Odd | EnumFlags.Even, "D");
					builder.Where<string>("Field3", "abc", "D");
				},
				"WHERE [D].[Field] = @D_Field AND ([D].[Field2] & @D_Field2_1 <> 0 OR [D].[Field2] & @D_Field2_2 <> 0) AND [D].[Field3] = @D_Field3",
				new[] { "D_Field", "D_Field2_1", "D_Field2_2", "D_Field3" }
			);

		[Fact]
		public void WhereBetweenTest()
			=> Test(
				builder => builder.WhereBetween<int>("Field", 1, 2, "D"),
				"WHERE [D].[Field] >= @D_FieldFrom AND [D].[Field] <= @D_FieldTo",
				new[] { "D_FieldFrom", "D_FieldTo" }
			);
	}
}
