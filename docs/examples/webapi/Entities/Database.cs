using Microsoft.Data.SqlClient;
using Simpleverse.Repository.Db;
using Simpleverse.Repository.Db.Entity;
using System.Data.Common;
using webapi.Entities.Basic;
using webapi.Entities.MultiModel;

namespace webapi.Entities
{
	public class Database : DbRepository
	{
		public static readonly Table<BasicModel> Basic = new("B");
		public static readonly Table<ParentModel> Parent = new("P");
		public static readonly Table<ChildModel> Child = new("C");

		public Database(Func<DbConnection> connectionFactory)
			: base(connectionFactory)
		{
		}
	}

	public static class RepositoryExtensions
	{
		public static IServiceCollection AddRepository(this IServiceCollection services, string connectionString)
		{
			services.AddSingleton(new Database(() => new SqlConnection(connectionString)));

			services.AddSingleton<IEntity<BasicModel>>(provider => new Entity<BasicModel>(provider.GetService<Database>(), Database.Basic));
			services.AddSingleton<IProjectedEntity<Basic.Projection.Basic, BasicModel>>(
				provider => new ProjectedEntity<Basic.Projection.Basic, BasicModel>(provider.GetService<IEntity<BasicModel>>())
			);

			return services;
		}
	}
}
