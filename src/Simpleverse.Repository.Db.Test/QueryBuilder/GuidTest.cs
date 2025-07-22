using System;
using System.Collections.Generic;
using Xunit;

namespace Simpleverse.Repository.Db.Test.QueryBuilder
{
	public class GuidTest : QueryBuilderTestFixture
	{
		public Guid Value { get; } = Guid.NewGuid();

		[Fact]
		public void TestGuid_Equals()
			=> Test<Model>(
				queryBuilder => queryBuilder.Where(x => x.Guid, Value),
				$"WHERE [Guid] = @Guid",
				new[] { "Guid" }
			);

		[Fact]
		public void TestGuid_In()
			=> Test<Model>(
				queryBuilder => queryBuilder.Where(x => x.Guid, new List<Guid>() { Value, Value }),
				$"WHERE [Guid] IN ('{Value}','{Value}')",
				Array.Empty<string>()
			);

		[Fact]
		public void TestGuid_NotIn()
		=> Test<Model>(
			queryBuilder => queryBuilder.WhereNot(x => x.Guid, new List<Guid>() { Value, Value }),
			$"WHERE [Guid] NOT IN ('{Value}','{Value}')",
			Array.Empty<string>()
		);
	}
}
