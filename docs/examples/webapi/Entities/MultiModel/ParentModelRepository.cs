using MoreLinq;
using Simpleverse.Repository.Db;
using Simpleverse.Repository.Db.Entity;
using System.Data;
using System.Reflection;
using System.Text.Json;

namespace webapi.Entities.MultiModel
{
	public class ParentModelRepository : Entity<ParentModel, IParentModel, DbQueryOptions>
	{
		private readonly IEntity<ChildModel> _childRepository;

		public ParentModelRepository(DbRepository repository, IEntity<ChildModel> childRepository)
			: base(repository, Database.Parent)
		{
			_childRepository = childRepository;
		}

		public override Task<int> AddAsync(
			IDbConnection connection,
			IEnumerable<ParentModel> models,
			Action<IEnumerable<ParentModel>, IEnumerable<ParentModel>, IEnumerable<PropertyInfo>, IEnumerable<PropertyInfo>> outputMap = null,
			IDbTransaction transaction = null
		)
		{
			return connection.ExecuteAsyncWithTransaction(
				async (conn, tran) =>
				{
					int count = await base.AddAsync(
						conn,
						models,
						outputMap: outputMap,
						transaction: tran
					);

					var children = models.SelectMany(
						x =>
						{
							x.Children.ForEach(child => child.ParentId = x.Id);
							return x.Children;
						}
					);

					await _childRepository.AddAsync(
						conn,
						children,
						outputMap: OutputMapper.Map,
						transaction: tran
					);

					return count;
				},
				transaction: transaction
			);
		}

		public override Task<int> UpdateAsync(
			IDbConnection connection,
			IEnumerable<ParentModel> models,
			Action<IEnumerable<ParentModel>, IEnumerable<ParentModel>, IEnumerable<PropertyInfo>, IEnumerable<PropertyInfo>> outputMap = null,
			IDbTransaction transaction = null
		)
		{
			return connection.ExecuteAsyncWithTransaction(
				async (conn, tran) =>
				{
					int count = await base.UpdateAsync(
						conn,
						models,
						OutputMapper.Map,
						transaction: tran
					);

					var children = models.SelectMany(
						x =>
						{
							x.Children.ForEach(child => child.ParentId = x.Id);
							return x.Children;
						}
					);

					await _childRepository.UpsertAsync(
						conn,
						children,
						outputMap: OutputMapper.Map,
						transaction: tran
					);

					return count;
				},
				transaction: transaction
			);
		}

		protected override void SelectQuery(QueryBuilder<ParentModel> builder, IParentModel filter, DbQueryOptions options)
		{
			base.SelectQuery(builder, filter, options);

			var chilQuery = Database
				.Child
				.SelectAsJson(
					builder =>
					{
						builder.SelectAll();
						builder.Where($"{Database.Child.Column(x => x.ParentId)} = {Source.Column(x => x.Id)}");
					}
				);

			builder.Select(x => x.Id);
			builder.Select($"({chilQuery.RawSql}) AS Children");
		}

		public override async Task<IEnumerable<T>> ListAsync<T>(
			IDbConnection connection,
			IParentModel filter,
			DbQueryOptions options,
			IDbTransaction transaction = null
		)
		{
			var results = await base.ListAsync<(T Model, (int Id, string Json) Children)>(connection, filter, options, transaction);

			return results
				.Select(
					x =>
					{
						if (x.Model is ParentModel parent)
						{
							parent.Children = JsonSerializer.Deserialize<IEnumerable<ChildModel>>(x.Children.Json);
						}

						return x.Model;
					}
				);
		}
	}
}
