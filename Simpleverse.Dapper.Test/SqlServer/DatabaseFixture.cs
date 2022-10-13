using System;
using Microsoft.Data.SqlClient;
using Dapper;

namespace Simpleverse.Dapper.Test.SqlServer
{
	public class DatabaseFixture : IDisposable
	{
		private static string ConnectionString = "Server=(localdb)\\mssqllocaldb;Database=tempdb;Trusted_Connection=True;MultipleActiveResultSets=true;";

		public SqlConnection GetConnection() => new SqlConnection(ConnectionString);

		private static string DropTable(string name) => $"IF OBJECT_ID('{name}', 'U') IS NOT NULL DROP TABLE {name};";
		private static string DropSchema(string name) => $"IF EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'{name}') DROP SCHEMA {name}";

		public void Dispose()
		{
			TearDownDb();
		}

		public DatabaseFixture()
		{
			SetupDb();
		}

		public void SetupDb()
		{
			using (var connection = new SqlConnection(ConnectionString))
			{
				connection.Open();

				connection.Execute(
					$@"{DropTable("[Identity]")}
					CREATE TABLE [Identity]
					(
						[Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
						[Name] NVARCHAR(MAX) NULL
					);");

				connection.Execute(
					$@"{DropTable("ExplicitKey")}
					CREATE TABLE ExplicitKey(
						[Id] INT NOT NULL PRIMARY KEY,
						[Name] NVARCHAR(MAX) NULL
					);");

				connection.Execute(
					$@"{DropTable("IdentityAndExplict")}
					CREATE TABLE IdentityAndExplict
					(
						[Id] INT IDENTITY(1,1) NOT NULL,
						[ExplicitKeyId] UNIQUEIDENTIFIER NOT NULL,
						[Name] NVARCHAR(MAX) NULL,
						CONSTRAINT [PK_IdentityAndExplict] PRIMARY KEY ([Id], [ExplicitKeyId]),
					);");

				connection.Execute(
					$@"{DropTable("Computed")}
					CREATE TABLE Computed
					(
						[Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
						[Name] NVARCHAR(MAX) NULL,
						[Value] INT NULL DEFAULT 5,
						[ValueDate] DATETIME NULL DEFAULT '2022-05-02',
						[ValueComputed] AS [Value] * 2
					);");

				connection.Execute(
					$@"{DropTable("Write")}
					CREATE TABLE Write
					(
						[Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
						[Name] NVARCHAR(MAX) NULL,
						[Ignored] INT NULL,
						[NotIgnored] INT NULL
					);");

				connection.Execute(
					$@"{DropTable("DataType")}
					CREATE TABLE DataType
					(
						[Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
						[Name] NVARCHAR(MAX) NULL,
						[Enum] INT NULL,
						[Guid] UNIQUEIDENTIFIER NULL,
						[DateTime] DATETIME NULL
					);");

				connection.Execute(
					$@"{DropTable("[10_Escape]")}
					CREATE TABLE [10_Escape](
						[NoId] INT NOT NULL,
					);");

				var schemaName = "test";

				connection.Execute(
				$@"{DropTable($@"{schemaName}.[10_Escape]")}");

				connection.Execute(
					$@"{DropSchema(schemaName)}");

				connection.Execute(
					$@"CREATE SCHEMA {schemaName}");

				connection.Execute(
					$@"CREATE TABLE [{schemaName}].[10_Escape](
						[NoId] INT NOT NULL
					);");
			}
		}

		public void TearDownDb()
		{
			using (var connection = new SqlConnection(ConnectionString))
			{
				connection.Open();
				connection.Execute($@"{DropTable("[Identity]")}");
				connection.Execute($@"{DropTable("[ExplicitKey]")}");
				connection.Execute($@"{DropTable("[IdentityAndExplict]")}");
				connection.Execute($@"{DropTable("[Computed]")}");
				connection.Execute($@"{DropTable("[Write]")}");
				connection.Execute($@"{DropTable("[DataType]")}");
				connection.Execute($@"{DropTable("[10_Escape]")}");

				var schemaName = "test";

				connection.Execute($@"{DropTable($@"{schemaName}.[10_Escape]")}");
				connection.Execute($@"{DropSchema(schemaName)}");
			}
		}
	}
}
