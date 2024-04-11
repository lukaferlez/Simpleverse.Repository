using Dapper.Contrib.Extensions;

namespace Simpleverse.Repository.Db.Test.SqlServer.Entity
{
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
