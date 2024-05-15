using Dapper.Contrib.Extensions;
using Simpleverse.Repository.Db.Entity;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Simpleverse.Repository.Db.Test.SqlServer.Entity
{
	[Collection("SqlServerCollection")]
	public class EntityProxyTest : TestFixture
	{
		public EntityProxyTest(ITestOutputHelper output)
			: base(output)
		{
		}

		[Fact]
		public async Task ListAsyncWithParametersTest()
		{
			// arange
			var repositoryHelper = new DapperHelper();
			var entity = new Entity(repositoryHelper.Instance());

			// act
			await entity.ListAsync(
				filter =>
				{
					filter.Name = "test";
					filter.Active = false;
				}
			);

			// assert
			var query = repositoryHelper.Query();
			Assert.Contains("[I].[Name] = @I_Name", query);
			Assert.Contains("[I].[Active] = @I_Active", query);
		}

		//[Fact]
		//public async Task ListAsync_WithNonVirtual_ShouldThrowNotSupported()
		//{
		//	// arange
		//	var repositoryHelper = new DapperHelper();
		//	var entity = new EntityNonVirtual(repositoryHelper.Instance());

		//	// act
		//	await Assert.ThrowsAsync<NotSupportedException>(
		//		async () =>
		//		{
		//			await entity.ListAsync(
		//				filter =>
		//				{
		//					filter.Name = "test";
		//					filter.Active = false;
		//				}
		//			);
		//		}
		//	);
		//}

		[Fact]
		public async Task ListAsync_WithNonVirtual_WithParametersTest()
		{
			// arange
			Settings.ForceUseOfVirtualProperties = false;
			var repositoryHelper = new DapperHelper();
			var entity = new EntityNonVirtual(repositoryHelper.Instance());

			// act
			await entity.ListAsync(
				filter =>
				{
					filter.Name = "test";
					filter.Active = false;
				}
			);

			// assert
			var query = repositoryHelper.Query();
			Assert.Contains("[I].[Name] = @I_Name", query);
			Assert.Contains("[I].[Active] = @I_Active", query);

			// cleanup
			Settings.ForceUseOfVirtualProperties = true;
		}

		[Fact]
		public async Task UpdateAsyncWithParametersTest()
		{
			// arange
			var repositoryHelper = new DapperHelper();
			var entity = new Entity(repositoryHelper.Instance());

			// act
			await entity.UpdateAsync(
				update =>
				{
					update.Name = "test";
				},
				filter =>
				{
					filter.Active = false;
				}
			);

			// assert
			var query = repositoryHelper.Query();
			Assert.Contains("[I].[Name] = @Set_I_Name", query);
			Assert.Contains("[I].[Active] = @I_Active", query);
		}

		[Fact]
		public async Task ListAsyncWithExtendParametersTest()
		{
			// arange
			var repositoryHelper = new DapperHelper();
			var entity = new EntityExtend(repositoryHelper.Instance());

			// act
			await entity.ListAsync(
				filter =>
				{
					filter.Name = "test";
					filter.Active = false;
					filter.DummyValue = 1;
				}
			);

			// assert
			var query = repositoryHelper.Query();
			Assert.Contains("[I].[Name] = @I_Name", query);
			Assert.Contains("[I].[Active] = @I_Active", query);
			Assert.Contains("[I].[DummyValue] = @I_DummyValue", query);
		}

		[Fact]
		public async Task UpdateAsyncWithExtendedParametersTest()
		{
			// arange
			var repositoryHelper = new DapperHelper();
			var entity = new EntityExtend(repositoryHelper.Instance());

			// act
			await entity.UpdateAsync(
				update =>
				{
					update.Name = "test";
					update.Description = "test";
				},
				filter =>
				{
					filter.DummyValue = 1;
					filter.Active = false;
				}
			);

			// assert
			var query = repositoryHelper.Query();
			Assert.Contains("[I].[Name] = @Set_I_Name", query);
			Assert.Contains("[I].[Description] = @Set_I_Description", query);
			Assert.Contains("[I].[Active] = @I_Active", query);
			Assert.Contains("[I].[DummyValue] = @I_DummyValue", query);
		}

		[Fact]
		public async Task ListAsyncWithCustomParametersTest()
		{
			// arange
			var repositoryHelper = new DapperHelper();
			var entity = new EntityCustom(repositoryHelper.Instance());

			// act
			await entity.ListAsync(
				filter =>
				{
					filter.Name = "test";
					filter.Active = false;
					filter.DummyValue = 1;
				}
			);

			// assert
			var query = repositoryHelper.Query();
			Assert.Contains("[I].[Name] = @I_Name", query);
			Assert.Contains("[I].[Active] = @I_Active", query);
			Assert.Contains("[I].[DummyValue] = @I_DummyValue", query);
		}

		[Fact]
		public async Task UpdateAsyncWithCustomParametersTest()
		{
			// arange
			var repositoryHelper = new DapperHelper();
			var entity = new EntityCustom(repositoryHelper.Instance());

			// act
			await entity.UpdateAsync(
				update =>
				{
					update.Name = "test";
					update.Description = "test";
				},
				filter =>
				{
					filter.DummyValue = 1;
					filter.Active = false;
				}
			);

			// assert
			var query = repositoryHelper.Query();
			Assert.Contains("[I].[Name] = @Set_I_Name", query);
			Assert.Contains("[I].[Description] = @Set_I_Description", query);
			Assert.Contains("[I].[Active] = @I_Active", query);
			Assert.Contains("[I].[DummyValue] = @I_DummyValue", query);
		}

		[Fact]
		public async Task UpdateAsyncWithExtendedInterfaceParametersTest()
		{
			// arange
			var repositoryHelper = new DapperHelper();
			var entity = new EntityInterfaceExtended(repositoryHelper.Instance());

			// act
			await entity.UpdateAsync(
				update =>
				{
					update.Name = "test";
					update.Description = "test";
				},
				filter =>
				{
					filter.DummyValue = 1;
					filter.Active = false;
				}
			);

			// assert
			var query = repositoryHelper.Query();
			Assert.Contains("[I].[Name] = @Set_I_Name", query);
			Assert.Contains("[I].[Description] = @Set_I_Description", query);
			Assert.Contains("[I].[Active] = @I_Active", query);
			Assert.Contains("[I].[DummyValue] = @I_DummyValue", query);
		}
	}

	public interface IEntityModel
	{
		int Id { get; set; }
		string Name { get; set; }
		bool Active { get; set; }
	}

	public class Entity : Entity<EntityModel>
	{
		public Entity(DbRepository repository)
			: base(repository, new Table<EntityModel>("I"))
		{
		}
	}

	[Table("IEntity")]
	public class EntityModel : IEntityModel
	{
		public virtual int Id { get; set; }
		public virtual string Name { get; set; }
		public virtual bool Active { get; set; }
	}

	public class EntityNonVirtual : Entity<EntityNonVirtualModel>
	{
		public EntityNonVirtual(DbRepository repository)
			: base(repository, new Table<EntityNonVirtualModel>("I"))
		{
		}

		protected override void Filter(QueryBuilder<EntityNonVirtualModel> builder, EntityNonVirtualModel filter)
		{
			base.Filter(builder, filter);
			builder.Where(x => x.Id, filter.Id);
			builder.Where(x => x.Name, filter.Name);
			builder.Where(x => x.Active, filter.Active);
		}
	}

	[Table("IEntity")]
	public class EntityNonVirtualModel
	{
		public int? Id { get; set; }
		public string Name { get; set; }
		public bool? Active { get; set; }
	}

	public class EntityExtend : Entity<EntityModelExtended>
	{
		public EntityExtend(DbRepository repository)
			: base(repository, new Table<EntityModelExtended>("I"))
		{
		}
	}

	public interface IEntityModelExtended : IEntityModel
	{
		string Description { get; set; }
		int DummyValue { get; set; }
	}

	public class EntityModelExtended : EntityModel, IEntityModelExtended
	{
		public string NormalizedName => Name.ToUpper();
		public virtual string Description { get; set; }
		public virtual int DummyValue { get; set; }
	}

	public class EntityCustom : Entity<EntityModelExtended>
	{
		public EntityCustom(DbRepository repository)
			: base(repository, new Table<EntityModelExtended>("I"))
		{
		}

		protected override void Filter(QueryBuilder<EntityModelExtended> builder, EntityModelExtended filter)
		{
			base.Filter(builder, filter);
			IfChanged(filter, x => x.DummyValue, () => builder.Where(x => x.DummyValue, filter.DummyValue));
		}
	}

	public class EntityInterfaceExtended : Entity<EntityModelExtended, IEntityModelExtended, DbQueryOptions>
	{
		public EntityInterfaceExtended(DbRepository repository)
			: base(repository, new Table<EntityModelExtended>("I"))
		{
		}
	}
}
