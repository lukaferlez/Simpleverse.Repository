using Xunit;

namespace Simpleverse.Repository.Db.Test.Selector
{
	public class SelectorCastTest
	{
		[Fact]
		public void Cast_WithSimpleColumn_ProducesCastExpression()
		{
			var selector = new Db.Selector("ColumnName", "t");
			selector.Cast("NVARCHAR(100)");
			Assert.Equal("CAST([t].[ColumnName] AS NVARCHAR(100))", selector.ToString());
		}

		[Fact]
		public void Cast_WithRawSource_ProducesCastExpression()
		{
			var selector = new Db.Selector("some_expression");
			selector.Cast("INT");
			Assert.Equal("CAST(some_expression AS INT)", selector.ToString());
		}

		[Fact]
		public void Cast_ReturnsSelector_ForChaining()
		{
			var selector = new Db.Selector("ColumnName", "t");
			var result = selector.Cast("INT");
			Assert.Same(selector, result);
		}

		[Fact]
		public void Cast_ChainedWithCount_WrapsInOrder()
		{
			var selector = new Db.Selector("ColumnName", "t");
			selector.Count().Cast("BIGINT");
			Assert.Equal("CAST(COUNT([t].[ColumnName]) AS BIGINT)", selector.ToString());
		}

		[Fact]
		public void Cast_ChainedBeforeCount_WrapsInOrder()
		{
			var selector = new Db.Selector("ColumnName", "t");
			selector.Cast("BIGINT").Count();
			Assert.Equal("COUNT(CAST([t].[ColumnName] AS BIGINT))", selector.ToString());
		}

		[Fact]
		public void Cast_WithAlias_ToStringWithAliasIncludesAlias()
		{
			var selector = new Db.Selector("ColumnName", "t");
			selector.Cast("NVARCHAR(50)").As("Alias");
			Assert.Equal("CAST([t].[ColumnName] AS NVARCHAR(50)) AS Alias", selector.ToString(withAlias: true));
		}

		[Fact]
		public void Cast_WithAlias_ToStringWithoutAliasExcludesAlias()
		{
			var selector = new Db.Selector("ColumnName", "t");
			selector.Cast("NVARCHAR(50)").As("Alias");
			Assert.Equal("CAST([t].[ColumnName] AS NVARCHAR(50))", selector.ToString());
		}

		[Fact]
		public void Cast_AppliedMultipleTimes_NestsCasts()
		{
			var selector = new Db.Selector("ColumnName", "t");
			selector.Cast("VARCHAR(10)").Cast("NVARCHAR(10)");
			Assert.Equal("CAST(CAST([t].[ColumnName] AS VARCHAR(10)) AS NVARCHAR(10))", selector.ToString());
		}
	}
}
