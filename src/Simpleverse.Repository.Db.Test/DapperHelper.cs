using Dapper;
using Moq;
using Moq.Dapper;
using Simpleverse.Repository.Db;
using Simpleverse.Repository.Db.SqlServer;
using Simpleverse.Repository.Db.Test.SqlServer.Entity;
using System;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace Simpleverse.Repository.Db.Test
{
    public class DapperHelper
	{
		private readonly Mock<DbConnection> _mockConnection;

		public DapperHelper()
		{
			_mockConnection = new Mock<DbConnection>();
			_mockConnection
				.SetupDapperAsync(c => c.QueryAsync<EntityModel>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<IDbTransaction>(), null, null))
				.ReturnsAsync(Array.Empty<EntityModel>());
		}

		public SqlRepository Instance()
			=> new SqlRepository(() => _mockConnection.Object);

		private IDbCommand LastDbCommandInvocation()
		{
			var invocation = _mockConnection
				.Invocations
				.LastOrDefault(x => x.Method.Name == nameof(IDbConnection.CreateCommand));

			if (invocation == null)
				return null;

			return (IDbCommand)invocation.ReturnValue;
		}

		public string Query()
		{
			var command = LastDbCommandInvocation();
			if (command == null)
				return string.Empty;

			return command.CommandText;
		}

		public IDataParameterCollection Parameters()
		{
			var command = LastDbCommandInvocation();
			if (command == null)
				return null;

			return command.Parameters;
		}
	}
}
