using Dapper.Contrib.Extensions;
using Simpleverse.Repository.Db.Entity;
using Simpleverse.Repository.Db.Extensions;
using Simpleverse.Repository.Db.Extensions.Dapper;
using Simpleverse.Repository.Db.SqlServer;
using StackExchange.Profiling.Data;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Xunit;
using Xunit.Abstractions;

namespace Simpleverse.Repository.Db.Test.SqlServer.Entity
{
	[Collection("SqlServerCollection")]
	public class EntityTest : DatabaseTestFixture
	{
		private readonly SqlRepository _sqlRepository;

		public EntityTest(DatabaseFixture fixture, ITestOutputHelper output)
			: base(fixture, output)
		{
			_sqlRepository = new SqlRepository(() => (ProfiledDbConnection)fixture.GetProfiledConnection());
		}

		[Fact]
		public void AddAsyncTest()
		{
			using (var profiler = Profile())
			using (var connection = _fixture.GetProfiledConnection())
			{
				// arange
				connection.Open();
				connection.Truncate<Identity>();
				var identityRecords = TestData.IdentityWithoutIdData(10);
				var entity = new IdentityEntity(_fixture);

				// act
				var recordCount = entity.AddAsync(identityRecords).Result;

				// assert
				var records = connection.GetAll<Identity>();
				Assert.Equal(10, recordCount);
				Assert.Equal(10, records.Count());
			}
		}

		[Fact]
		public void AddAsyncWithAmbientTest()
		{
			using (var profiler = Profile())
			using (var connection = _fixture.GetProfiledConnection())
			{
				// arange
				connection.Open();
				connection.Truncate<Identity>();
			}

			var entity = new IdentityEntity(_fixture);
			var identityRecords = TestData.IdentityWithoutIdData(10);

			// act
			var recordCount = 0;
			using (
					var ts = new TransactionScope(
						TransactionScopeOption.Required,
						new TransactionOptions() { IsolationLevel = IsolationLevel.ReadCommitted },
						TransactionScopeAsyncFlowOption.Enabled
					)
				)
			{
				recordCount = entity.AddAsync(identityRecords).Result;
				ts.Complete();
			}

			// assert
			using (var connection = _fixture.GetProfiledConnection())
			{

				var records = connection.GetAll<Identity>();
				Assert.Equal(10, recordCount);
				Assert.Equal(10, records.Count());
			}
		}

		[Fact]
		public void ListAsyncTupleTest()
		{
			using (var profiler = Profile())
			using (var connection = _fixture.GetProfiledConnection())
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
			var exception = Assert.ThrowsAsync<NotSupportedException>(
					() => entity.ListAsync<(
						Identity identity1,
						Identity identity2,
						Identity identity3,
						Identity identity4,
						Identity identity5,
						Identity identity6,
						Identity identity7,
						Identity identity8
					)>()
				).Result;

			Assert.Equal("Number of Tuple arguments is more than the supported 7.", exception.Message);
		}

		[Fact]
		public async Task AddAsync_WhenProvidedSqlRepository_InsertsRecords()
		{
			using (var profiler = Profile())
			using (var connection = _fixture.GetProfiledConnection())
			{
				// arange
				connection.Open();
				connection.Truncate<Identity>();
				var identityRecords = TestData.IdentityWithoutIdData(10);
				var entity = new IdentityEntity(_sqlRepository);

				// act
				var recordCount = await entity.AddAsync(identityRecords);

				// assert
				var records = connection.GetAll<Identity>();
				Assert.Equal(10, recordCount);
				Assert.Equal(10, records.Count());
			}
		}

		[Theory]
		[InlineData(10)]
		public async Task GetAsync_WhenProvidedSqlRepository_ReturnsRecord(int count)
		{
			using (var profiler = Profile())
			using (var connection = _fixture.GetProfiledConnection())
			{
				// arange
				connection.Open();
				connection.Truncate<Identity>();
				connection.Truncate<ExplicitKey>();
				var explicitKeyRecords = TestData.ExplicitKeyData(count);
				var identityRecords = TestData.IdentityWithIdData(count);
				connection.Insert(explicitKeyRecords);
				connection.Insert(identityRecords);
				var entity = new IdentityEntity(_sqlRepository);

				// act
				var recordCount = await entity.AddAsync(identityRecords);
				var returnedEntity = await entity.GetAsync();
				// assert
				Assert.Equal(count, recordCount);
				Assert.NotNull(returnedEntity);
			}
		}

		[Theory]
		[InlineData(10)]
		public async Task ListAsync_WhenProvidedSqlRepository_ReturnsRecords(int count)
		{
			using (var profiler = Profile())
			using (var connection = _fixture.GetProfiledConnection())
			{
				// arange
				connection.Open();
				connection.Truncate<Identity>();
				connection.Truncate<ExplicitKey>();
				var explicitKeyRecords = TestData.ExplicitKeyData(count);
				var identityRecords = TestData.IdentityWithIdData(count);
				connection.Insert(explicitKeyRecords);
				connection.Insert(identityRecords);
				var entity = new IdentityEntity(_sqlRepository);

				// act
				var recordCount = await entity.AddAsync(identityRecords);
				var returnedEntity = await entity.ListAsync();
				// assert
				Assert.Equal(count, recordCount);
				Assert.NotNull(returnedEntity);
			}
		}

		[Theory]
		[InlineData(10)]
		public async Task UpsertAsync_WhenProvidedSqlRepositoryAndIdentityDoesntExist_InsertsRecord(int count)
		{
			using (var profiler = Profile())
			using (var connection = _fixture.GetProfiledConnection())
			{
				// arange
				connection.Open();
				connection.Truncate<Identity>();
				connection.Truncate<ExplicitKey>();
				var explicitKeyRecords = TestData.ExplicitKeyData(count);
				connection.Insert(explicitKeyRecords);
				var entity = new IdentityEntity(_sqlRepository);
				var identityToInsert = new Identity
				{
					Name = Guid.NewGuid().ToString()
				};
				// act
				identityToInsert.Id = await entity.UpsertAsync(identityToInsert);
				var fetchedIdentity = await entity.GetAsync(filter => filter.Name = identityToInsert.Name);
				// assert
				Assert.NotEqual(0, identityToInsert.Id);
				Assert.NotNull(fetchedIdentity);
			}
		}

		[Theory]
		[InlineData(10)]
		public async Task UpsertAsync_WhenProvidedSqlRepositoryAndIdentityExists_UpdatesRecord(int count)
		{
			using (var profiler = Profile())
			using (var connection = _fixture.GetProfiledConnection())
			{
				// arange
				connection.Open();
				connection.Truncate<Identity>();
				connection.Truncate<ExplicitKey>();
				var explicitKeyRecords = TestData.ExplicitKeyData(count);
				connection.Insert(explicitKeyRecords);
				var entity = new IdentityEntity(_sqlRepository);
				var identityToInsert = new Identity
				{
					Name = Guid.NewGuid().ToString()
				};
				// act
				identityToInsert.Id = await entity.AddAsync(identityToInsert);
				identityToInsert.Name = Guid.NewGuid().ToString();
				await entity.UpsertAsync(identityToInsert);
				var fetchedIdentity = await entity.GetAsync(filter => filter.Name = identityToInsert.Name);
				// assert
				Assert.NotEqual(0, identityToInsert.Id);
				Assert.NotNull(fetchedIdentity);
			}
		}
	}

	public class IdentityEntity : Entity<Identity, IdentityQueryFilter, DbQueryOptions>
	{
		public IdentityEntity(DatabaseFixture fixture)
			: base(new DbRepository(() => fixture.GetProfiledConnection()), new Table<Identity>("I"))
		{
		}

		public IdentityEntity(SqlRepository sqlRepository) : base(sqlRepository, new Table<Identity>("I")) { }

		protected override void SelectQuery(QueryBuilder<Identity> builder, IdentityQueryFilter filter, DbQueryOptions options)
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

		protected override void Filter(QueryBuilder<Identity> builder, IdentityQueryFilter filter)
		{
			builder.Where(x => x.Name, filter.Name);
			base.Filter(builder, filter);
		}
	}

	public class IdentityQueryFilter
	{
		public virtual string Name { get; set; }
	}
}
