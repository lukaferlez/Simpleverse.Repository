using Simpleverse.Repository.Db.Extensions;
using Simpleverse.Repository.Db.SqlServer;
using StackExchange.Profiling.Data;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Simpleverse.Repository.Db.Test.SqlServer.Entity
{
	[Collection("SqlServerCollection")]
	public class CancellationTokenTests : DatabaseTestFixture
	{
		private readonly SqlRepository _sqlRepository;

		public CancellationTokenTests(DatabaseFixture fixture, ITestOutputHelper output)
			: base(fixture, output)
		{
			_sqlRepository = new SqlRepository(() => (ProfiledDbConnection)fixture.GetProfiledConnection());
		}

		[Fact]
		public async Task ListAsync_WhenCancellationTokenCancelled_ThrowsOperationCanceledException()
		{
			using (var profiler = Profile())
			using (var connection = _fixture.GetProfiledConnection())
			{
				// arrange
				connection.Open();
				connection.Truncate<Identity>();
				var entity = new IdentityEntity(_sqlRepository);
				var cancelledToken = new CancellationToken(canceled: true);

				// act & assert
				await Assert.ThrowsAnyAsync<OperationCanceledException>(
					() => entity.ListAsync(cancellationToken: cancelledToken)
				);
			}
		}

		[Fact]
		public async Task GetAsync_WhenCancellationTokenCancelled_ThrowsOperationCanceledException()
		{
			using (var profiler = Profile())
			using (var connection = _fixture.GetProfiledConnection())
			{
				// arrange
				connection.Open();
				connection.Truncate<Identity>();
				var entity = new IdentityEntity(_sqlRepository);
				var cancelledToken = new CancellationToken(canceled: true);

				// act & assert
				await Assert.ThrowsAnyAsync<OperationCanceledException>(
					() => entity.GetAsync(cancellationToken: cancelledToken)
				);
			}
		}

		[Fact]
		public async Task AddAsync_WhenCancellationTokenCancelled_ThrowsOperationCanceledException()
		{
			using (var profiler = Profile())
			using (var connection = _fixture.GetProfiledConnection())
			{
				// arrange
				connection.Open();
				connection.Truncate<Identity>();
				var entity = new IdentityEntity(_sqlRepository);
				var records = TestData.IdentityWithoutIdData(1);
				var cancelledToken = new CancellationToken(canceled: true);

				// act & assert
				await Assert.ThrowsAnyAsync<OperationCanceledException>(
					() => entity.AddAsync(records, cancellationToken: cancelledToken)
				);
			}
		}

		[Fact]
		public async Task UpdateAsync_WhenCancellationTokenCancelled_ThrowsOperationCanceledException()
		{
			using (var profiler = Profile())
			using (var connection = _fixture.GetProfiledConnection())
			{
				// arrange
				connection.Open();
				connection.Truncate<Identity>();
				var entity = new IdentityEntity(_sqlRepository);
				var cancelledToken = new CancellationToken(canceled: true);

				// act & assert
				await Assert.ThrowsAnyAsync<OperationCanceledException>(
					() => entity.UpdateAsync(
						update => update.Name = "test",
						cancellationToken: cancelledToken
					)
				);
			}
		}

		[Fact]
		public async Task DeleteAsync_WhenCancellationTokenCancelled_ThrowsOperationCanceledException()
		{
			using (var profiler = Profile())
			using (var connection = _fixture.GetProfiledConnection())
			{
				// arrange
				connection.Open();
				connection.Truncate<Identity>();
				var entity = new IdentityEntity(_sqlRepository);
				var cancelledToken = new CancellationToken(canceled: true);

				// act & assert
				await Assert.ThrowsAnyAsync<OperationCanceledException>(
					() => entity.DeleteAsync(cancellationToken: cancelledToken)
				);
			}
		}

		[Fact]
		public async Task ExistsAsync_WhenCancellationTokenCancelled_ThrowsOperationCanceledException()
		{
			using (var profiler = Profile())
			using (var connection = _fixture.GetProfiledConnection())
			{
				// arrange
				connection.Open();
				connection.Truncate<Identity>();
				var entity = new IdentityEntity(_sqlRepository);
				var cancelledToken = new CancellationToken(canceled: true);

				// act & assert
				await Assert.ThrowsAnyAsync<OperationCanceledException>(
					() => entity.ExistsAsync(cancellationToken: cancelledToken)
				);
			}
		}

		[Fact]
		public async Task DbRepository_QueryAsync_WhenCancellationTokenCancelled_ThrowsOperationCanceledException()
		{
			using (var profiler = Profile())
			{
				// arrange
				var cancelledToken = new CancellationToken(canceled: true);

				// act & assert
				await Assert.ThrowsAnyAsync<OperationCanceledException>(
					() => _sqlRepository.QueryAsync<Identity>("SELECT 1", null, cancelledToken)
				);
			}
		}

		[Fact]
		public async Task DbRepository_ExecuteAsync_WhenCancellationTokenCancelled_ThrowsOperationCanceledException()
		{
			using (var profiler = Profile())
			{
				// arrange
				var builder = new QueryBuilder<Identity>();
				var query = builder.AddTemplate("SELECT 1");
				var cancelledToken = new CancellationToken(canceled: true);

				// act & assert
				await Assert.ThrowsAnyAsync<OperationCanceledException>(
					() => _sqlRepository.ExecuteAsync(query, cancelledToken)
				);
			}
		}
	}
}
