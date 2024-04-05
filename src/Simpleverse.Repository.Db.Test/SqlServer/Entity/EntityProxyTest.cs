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
	}
}
