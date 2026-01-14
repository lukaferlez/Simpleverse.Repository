using Microsoft.Data.SqlClient;
using Simpleverse.Repository.Db.SqlServer;
using StackExchange.Profiling.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Simpleverse.Repository.Db.Test.SqlServer
{
	[Collection("SqlServerCollection")]
	public class ReleaseAppLockTests : DatabaseTestFixture
	{
		public ReleaseAppLockTests(DatabaseFixture fixture, ITestOutputHelper output)
			: base(fixture, output)
		{
		}

		[Fact]
		public async Task ReleaseAppLockAsync_WithTransaction_SuccessfullyReleasesLock()
		{
			using var connection = _fixture.GetProfiledConnection();
			connection.Open();
			var sqlConnection = (SqlConnection)((ProfiledDbConnection)connection).WrappedConnection;
			using var transaction = sqlConnection.BeginTransaction();
			var key = "test_lock_transaction_501";

			var lockAcquired = await sqlConnection.GetAppLockAsync(key, transaction: transaction);
			Assert.True(lockAcquired);

			// Act
			var result = await sqlConnection.ReleaseAppLockAsync(key, transaction: transaction);

			// Assert
			Assert.True(result);

			transaction.Commit();
		}

		[Fact]
		public async Task ReleaseAppLockAsync_WithSession_SuccessfullyReleasesLock()
		{
			using var connection = _fixture.GetProfiledConnection();
			connection.Open();
			var sqlConnection = (SqlConnection)((ProfiledDbConnection)connection).WrappedConnection;

			var key = "test_lock_session_502";
			var lockAcquired = await sqlConnection.GetAppLockAsync(key, transaction: null);
			Assert.True(lockAcquired);

			// Act
			var result = await sqlConnection.ReleaseAppLockAsync(key, transaction: null);

			// Assert
			Assert.True(result);
		}

		[Fact]
		public async Task ReleaseAppLockAsync_LockNotHeld_ThrowsSqlException()
		{
			using var connection = _fixture.GetProfiledConnection();
			connection.Open();
			var sqlConnection = (SqlConnection)((ProfiledDbConnection)connection).WrappedConnection;
			using var transaction = sqlConnection.BeginTransaction();
			var key = "test_lock_not_held_503";

			// Act & Assert
			await Assert.ThrowsAsync<SqlException>(async () =>
			{
				await sqlConnection.ReleaseAppLockAsync(key, transaction: transaction);
			});

			transaction.Rollback();
		}

		[Fact]
		public async Task ReleaseAppLockAsync_WrongLockOwner_ThrowsSqlException()
		{
			using var connection = _fixture.GetProfiledConnection();
			await connection.OpenAsync();
			var sqlConnection = (SqlConnection)((ProfiledDbConnection)connection).WrappedConnection;
			var key = "test_lock_wrong_owner_504";

			// Acquire lock at session level (without transaction)
			await sqlConnection.GetAppLockAsync(key, transaction: null);

			// Now create a transaction and try to release the session-level lock
			using var transaction = await sqlConnection.BeginTransactionAsync();

			// Act & Assert
			await Assert.ThrowsAsync<SqlException>(async () =>
			{
				await sqlConnection.ReleaseAppLockAsync(key, transaction: transaction);
			});

			// Cleanup
			await Assert.ThrowsAsync<InvalidOperationException>(async () =>
			{
				await sqlConnection.ReleaseAppLockAsync(key, transaction: null);
			});
			
			transaction.Rollback();
		}

		[Fact]
		public async Task ReleaseAppLockAsync_KeyTooLong_ThrowsArgumentOutOfRangeException()
		{
			using var connection = _fixture.GetProfiledConnection();
			connection.Open();
			var sqlConnection = (SqlConnection)((ProfiledDbConnection)connection).WrappedConnection;
			using var transaction = sqlConnection.BeginTransaction();
			var key = new string('a', 256);

			// Act & Assert
			await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
			{
				await sqlConnection.ReleaseAppLockAsync(key, transaction: transaction);
			});

			transaction.Rollback();
		}

		[Fact]
		public async Task ReleaseAppLockAsync_MultipleKeys_SuccessfullyReleasesAll()
		{
			using var connection = _fixture.GetProfiledConnection();
			connection.Open();
			var sqlConnection = (SqlConnection)((ProfiledDbConnection)connection).WrappedConnection;
			using var transaction = sqlConnection.BeginTransaction();
			var keys = new List<string> { "test_lock_505", "test_lock_506", "test_lock_507" };

			foreach (var key in keys)
			{
				await sqlConnection.GetAppLockAsync(key, transaction: transaction);
			}

			// Act
			var result = await sqlConnection.ReleaseAppLockAsync(keys, transaction: transaction);

			// Assert
			Assert.True(result);

			transaction.Commit();
		}

		[Fact]
		public async Task ReleaseAppLockAsync_MultipleKeys_SomeNotHeld_ThrowsSqlException()
		{
			using var connection = _fixture.GetProfiledConnection();
			connection.Open();
			var sqlConnection = (SqlConnection)((ProfiledDbConnection)connection).WrappedConnection;
			using var transaction = sqlConnection.BeginTransaction();
			var keys = new List<string> { "test_lock_508", "test_lock_not_held_509", "test_lock_510" };

			// Acquire only two of the three locks
			await sqlConnection.GetAppLockAsync(keys[0], transaction: transaction);
			await sqlConnection.GetAppLockAsync(keys[2], transaction: transaction);

			// Act & Assert - Should throw when trying to release lock that's not held
			await Assert.ThrowsAsync<SqlException>(async () =>
			{
				await sqlConnection.ReleaseAppLockAsync(keys, transaction: transaction);
			});

			transaction.Rollback();
		}

		[Fact]
		public async Task ReleaseAppLockAsync_AfterTransactionCommit_LockIsReleased()
		{
			using var connection = _fixture.GetProfiledConnection();
			connection.Open();
			var sqlConnection = (SqlConnection)((ProfiledDbConnection)connection).WrappedConnection;

			var key = "test_lock_auto_release_511";

			using (var transaction = sqlConnection.BeginTransaction())
			{
				await sqlConnection.GetAppLockAsync(key, transaction: transaction);
				transaction.Commit();
			}

			// Act & Assert
			using (var transaction = sqlConnection.BeginTransaction())
			{
				await Assert.ThrowsAsync<SqlException>(async () =>
				{
					await sqlConnection.ReleaseAppLockAsync(key, transaction: transaction);
				});

				transaction.Rollback();
			}
		}
	}
}