using Dapper.Contrib.Extensions;
using System.Linq;
using Simpleverse.Repository.Db.Extensions;
using Simpleverse.Repository.Db.Extensions.Dapper;
using Xunit;
using Xunit.Abstractions;
using System;

namespace Simpleverse.Repository.Db.Test.SqlServer.Entity
{
	[Collection("SqlServerCollection")]
	public class EntityTest : TestFixture
	{
		public EntityTest(DatabaseFixture fixture, ITestOutputHelper output)
			: base(fixture, output)
		{

		}

		[Fact]
		public void ListAsyncTupleTest()
		{
			using (var profiler = Profile())
			using (var connection = _fixture.GetConnection())
			{
				// arange
				connection.Open();
				connection.Truncate<Identity>();
				connection.Truncate<ExplicitKey>();
				var explicitKeyRecords = TestData.ExplicitKeyData(10);
				var identityRecords = TestData.IdentityWithIdData(10);

				connection.Insert(explicitKeyRecords);
				connection.Insert(identityRecords);

				var entity = new IdentityEntity(_fixture);

				// act
				var records = entity.ListAsync<(Identity identity, ExplicitKey explicitKey)>().Result;

				// assert
				Assert.Equal(10, records.Count());
				foreach (var record in records)
				{
					Assert.NotEqual(0, record.identity.Id);
					Assert.NotEqual(0, record.explicitKey.Id);
				}
			}
		}

		[Fact]
		public void ListAsyncTupleFailTest()
		{
			var entity = new IdentityEntity(_fixture);

			// act
			var exception = Assert.Throws<NotSupportedException>(
					() => entity.ListAsync<(
						Identity identity1,
						Identity identity2,
						Identity identity3,
						Identity identity4,
						Identity identity5,
						Identity identity6,
						Identity identity7,
						Identity identity8
					) >().Result
				);

			Assert.Equal("Number of Tuple arguments is more than the supported 7.", exception.Message);
		}
	}

	public class IdentityEntity : Entity<Identity, QueryFilter, DbQueryOptions>
	{
		public IdentityEntity(DatabaseFixture fixture)
			: base(new DbRepository(() => fixture.GetConnection()), new Table<Identity>("I"))
		{
		}

		protected override void SelectQuery(QueryBuilder<Identity> builder, QueryFilter filter, DbQueryOptions options)
		{
			var explicitKey = new Table<ExplicitKey>("EK");

			builder.SelectAll(explicitKey);

			builder.Join(
				explicitKey,
				i => i.Id,
				ek => ek.Id
			);

			base.SelectQuery(builder, filter, options);
		}
	}
}
