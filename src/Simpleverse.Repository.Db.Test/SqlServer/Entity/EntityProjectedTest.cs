using Simpleverse.Repository.Db.SqlServer;
using Simpleverse.Repository.Entity;
using StackExchange.Profiling.Data;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Simpleverse.Repository.Db.Test.SqlServer.Entity
{
	[Collection("SqlServerCollection")]
	public class EntityProjectedTest : DatabaseTestFixture
	{
		private readonly SqlRepository _sqlRepository;

		public EntityProjectedTest(DatabaseFixture fixture, ITestOutputHelper output)
			: base(fixture, output)
		{
			_sqlRepository = new SqlRepository(() => (ProfiledDbConnection)fixture.GetProfiledConnection());
		}

		[Fact]
		public async Task AddAsync_Single_IdEqauls1()
		{
			// arange
			var entity = new ProjectedIdentityEntity(_sqlRepository);
			var projection = new ProjectedIdentity() { Name = "Test" };

			// act
			await entity.AddAsync(projection);

			// assert
			Assert.Equal(1, projection.Model.Id);
		}

		[Fact]
		public async Task AddAsync_Multi_IdEqauls1()
		{
			// arange
			var entity = new ProjectedIdentityEntity(_sqlRepository);
			var projections = new[] {
				new ProjectedIdentity() { Name = "Test" },
				new ProjectedIdentity() { Name = "Tes2" }
			};

			// act
			await entity.AddAsync(projections);

			// assert
			Assert.NotEqual(0, projections.First().Model.Id);
			Assert.NotEqual(0, projections.ElementAt(1).Model.Id);
		}
	}

	public class ProjectedIdentity : IProject<Identity>
	{
		public Identity Model { get; }

		public ProjectedIdentity()
			: this(new Identity())
		{

		}

		public ProjectedIdentity(Identity model)
		{
			Model = model;
		}

		public string Name
		{
			get => Model.Name;
			set => Model.Name = value;
		}
	}

	internal class ProjectedIdentityEntity : ProjectedEntity<ProjectedIdentity, Identity, IdentityQueryFilter, DbQueryOptions>
	{
		public ProjectedIdentityEntity(SqlRepository repository)
			: this(repository, model => new ProjectedIdentity(model))
		{
		}

		public ProjectedIdentityEntity(SqlRepository repository, Func<Identity, ProjectedIdentity> creator)
			: base(new IdentityEntity(repository), creator)
		{
		}
	}
}
