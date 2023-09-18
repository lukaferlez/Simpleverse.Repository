using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Simpleverse.Repository.Db.Test.QueryBuilder
{
	public abstract class WhereTestFixture
	{
		public void Test(Action<SqlBuilder> testAction, string execptedSql, IEnumerable<string> expectedParamNames)
		{
			var queryBuilder = new SqlBuilder();
			var query = queryBuilder.AddTemplate("/**where**/");

			testAction(queryBuilder);

			Assert.Equal(execptedSql, query.RawSql.Trim());
			Assert.Equal(expectedParamNames.Count(), (query.Parameters as Dapper.DynamicParameters).ParameterNames.Count());
			for (int i = 0; i < expectedParamNames.Count(); i++)
			{
				Assert.Equal(expectedParamNames.ElementAt(i), (query.Parameters as Dapper.DynamicParameters).ParameterNames.ElementAt(i));
			}
		}

		public void Test<T>(Action<QueryBuilder<T>> testAction, string execptedSql, IEnumerable<string> expectedParamNames)
		{
			var queryBuilder = new QueryBuilder<T>();
			var query = queryBuilder.AddTemplate("/**where**/");

			testAction(queryBuilder);

			Assert.Equal(execptedSql, query.RawSql.Trim());
			Assert.Equal(expectedParamNames.Count(), (query.Parameters as Dapper.DynamicParameters).ParameterNames.Count());
			for (int i = 0; i < expectedParamNames.Count(); i++)
			{
				Assert.Equal(expectedParamNames.ElementAt(i), (query.Parameters as Dapper.DynamicParameters).ParameterNames.ElementAt(i));
			}
		}
	}
}
