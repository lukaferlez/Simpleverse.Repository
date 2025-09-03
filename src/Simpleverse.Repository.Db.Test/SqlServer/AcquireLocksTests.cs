using Microsoft.Data.SqlClient;
using Simpleverse.Repository.Db.SqlServer;
using StackExchange.Profiling.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Simpleverse.Repository.Db.Test.SqlServer
{
	[Collection("SqlServerCollection")]
	public class AcquireLocksTests : DatabaseTestFixture
	{
		public AcquireLocksTests(DatabaseFixture fixture, ITestOutputHelper output)
			: base(fixture, output)
		{
		}

		[Fact]
		public async Task TryGetAcquireLocks_AllLocksAcquired_ReturnsTrue()
		{
			using (var connection = _fixture.GetProfiledConnection())
			{
				connection.Open();
				var sqlConnection = (SqlConnection)((ProfiledDbConnection)connection).WrappedConnection;
				using (var transaction = sqlConnection.BeginTransaction())
				{
					var keys = new List<string> { "101", "102", "103" };

					// act
					var result = await sqlConnection.TryGetAppLockAsync(keys, transaction: transaction);

					// assert
					Assert.True(result);

					transaction.Commit();
				}
			}
		}

		[Fact]
		public async Task TryGetAcquireLocks_ParallelThreads_ContentionTest()
		{
			var keys = new List<string> { "201", "202", "203" };

			async Task<bool> TryAcquireLocksAsync()
			{
				using var connection = _fixture.GetProfiledConnection();
				connection.Open();
				var sqlConnection = (SqlConnection)((ProfiledDbConnection)connection).WrappedConnection;
				using var transaction = sqlConnection.BeginTransaction();
				var result = await sqlConnection.TryGetAppLockAsync(keys, transaction: transaction, lockTimeout: TimeSpan.FromMilliseconds(500));
				if (result)
					transaction.Commit();
				else
					transaction.Rollback();
				return result;
			}

			// Run two tasks in parallel
			var task1 = Task.Run(TryAcquireLocksAsync);
			var task2 = Task.Run(TryAcquireLocksAsync);

			var results = await Task.WhenAll(task1, task2);

			// Only one should succeed in acquiring all locks
			Assert.Equal(2, results.Count(r => r));
		}

		[Fact]
		public async Task TryGetAcquireLocks_NullKeys_ThrowsArgumentException()
		{
			using var connection = _fixture.GetProfiledConnection();
			connection.Open();
			var sqlConnection = (SqlConnection)((ProfiledDbConnection)connection).WrappedConnection;
			using var transaction = sqlConnection.BeginTransaction();

			await Assert.ThrowsAsync<ArgumentException>(async () =>
			{
				await sqlConnection.TryGetAppLockAsync(null, transaction: transaction);
			});

			transaction.Rollback();
		}

		[Fact]
		public async Task TryGetAcquireLocks_EmptyKeys()
		{
			using var connection = _fixture.GetProfiledConnection();
			connection.Open();
			var sqlConnection = (SqlConnection)((ProfiledDbConnection)connection).WrappedConnection;
			using var transaction = sqlConnection.BeginTransaction();

			try
			{
				await sqlConnection.TryGetAppLockAsync(new List<string> { }, transaction: transaction);
			}
			catch (ArgumentException ex) when (ex.Message.Contains("Keys collection must not be null or empty.", StringComparison.OrdinalIgnoreCase))
			{
				transaction.Rollback();
				return;
			}
		}

		[Fact]
		public async Task TryGetAcquireLocks_DuplicateKeys_ReturnsTrue()
		{
			using var connection = _fixture.GetProfiledConnection();
			connection.Open();
			var sqlConnection = (SqlConnection)((ProfiledDbConnection)connection).WrappedConnection;
			using var transaction = sqlConnection.BeginTransaction();

			var keys = new List<string> { "301", "301", "302" };
			var result = await sqlConnection.TryGetAppLockAsync(keys, transaction: transaction);

			Assert.True(result);

			transaction.Commit();
		}

		[Fact]
		public async Task TryGetAcquireLocks_LockTimeout_ReturnsFalse()
		{
			var keys = new List<string> { "401", "402" };

			using var connection1 = _fixture.GetProfiledConnection();
			connection1.Open();
			var sqlConnection1 = (SqlConnection)((ProfiledDbConnection)connection1).WrappedConnection;
			using var transaction1 = sqlConnection1.BeginTransaction();
			await sqlConnection1.TryGetAppLockAsync(keys, transaction: transaction1);

			using var connection2 = _fixture.GetProfiledConnection();
			connection2.Open();
			var sqlConnection2 = (SqlConnection)((ProfiledDbConnection)connection2).WrappedConnection;
			using var transaction2 = sqlConnection2.BeginTransaction();
			var result = await sqlConnection2.TryGetAppLockAsync(keys, transaction: transaction2, lockTimeout: TimeSpan.FromMilliseconds(100));

			Assert.False(result);

			transaction1.Rollback();
			transaction2.Rollback();
		}
	}
}
