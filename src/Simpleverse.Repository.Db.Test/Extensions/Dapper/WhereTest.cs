using Dapper;
using System.Linq;
using Xunit;
using Simpleverse.Repository.Db.Extensions.Dapper;

namespace Simpleverse.Repository.Db.Test.Extensions.Dapper
{
	public class WhereTest
	{

		[Fact]
		public void WhereBetweenTest()
		{
			SqlBuilder sqlBuilder = new SqlBuilder();
			var selector = sqlBuilder.AddTemplate(@"/**where**/");

			sqlBuilder.WhereBetween<int>("Field", 1, 2, "D");

			Assert.Equal("D_FieldFrom", ((DynamicParameters)selector.Parameters).ParameterNames.ElementAt(0));
			Assert.Equal("D_FieldTo", ((DynamicParameters)selector.Parameters).ParameterNames.ElementAt(1));
			Assert.Equal("WHERE [D].[Field] >= @D_FieldFrom AND [D].[Field] <= @D_FieldTo\n", selector.RawSql);
		}
	}
}
