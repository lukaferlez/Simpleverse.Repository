# Usage Guide

This guide covers the core concepts of `Simpleverse.Repository` / `Simpleverse.Repository.Db`
and walks through common scenarios such as entity mapping, configuring `TypeMeta`, and
querying data with `ListAsync`.

## Packages

- **Simpleverse.Repository** – Base, database-agnostic abstractions (`IEntity`, `IQueryList`,
  `IAdd`, `IUpdate`, `IUpsert`, `IReplace`, `IDelete`, etc.).
- **Simpleverse.Repository.Db** – SQL Server / Dapper implementation of those abstractions,
  including `DbRepository`, `SqlRepository`, `Table<T>`, `TypeMeta`, `QueryBuilder<T>`, bulk
  operations (Insert/Update/Delete/Upsert/Merge) and application locks.

Install via NuGet:

```
dotnet add package Simpleverse.Repository.Db
```

## Core concepts

- Repositories are agnostic to the underlying datasource, so the same pattern can be used
  whether the datasource is a DB, an API, a file system, etc. That is why the project is
  split into two packages – the goal is a single interface consumers use without needing to
  understand the underlying mechanics.
- Everything is type-safe and checkable at compile time – no strings or mappings involved,
  so errors are caught early.
- Implementation details are fully up to the entity itself, so consumers can write their own
  entity implementing the interface, backed by Entity Framework, direct SQL, stored
  procedures, etc.

### Entity mapping

Models are plain POCOs mapped with [Dapper.Contrib](https://github.com/DapperLib/Dapper.Contrib)
attributes:

```csharp
using Dapper.Contrib.Extensions;

[Table("[Identity]")]
public class Identity
{
    [Key]
    public virtual int Id { get; set; }

    public virtual string Name { get; set; }

    public virtual string From { get; set; }
}
```

Supported attributes:

- `[Table("name")]` – overrides the table name (schema qualified names are supported,
  e.g. `[Table("dbo.[Identity]")]`).
- `[Key]` – identity/auto-generated primary key.
- `[ExplicitKey]` – primary key supplied by the caller (not database generated).
- `[Computed]` – column is computed by the database and never written to.
- `[Write(false)]` – excludes the property from INSERT/UPDATE statements.
- `Simpleverse.Repository.Entity.ImmutableAttribute` – property can be inserted but is
  never updated afterwards.

### TypeMeta

`Simpleverse.Repository.Db.Meta.TypeMeta` reflects over a model type once and caches the
result, exposing the property groups used to build SQL (`Properties`, `PropertiesKey`,
`PropertiesComputed`, `PropertiesExceptKeyAndComputed`, etc.) as well as the resolved
`TableName`.

```csharp
using Simpleverse.Repository.Db.Meta;

var meta = TypeMeta.Get<Identity>();

meta.TableName;                       // "[Identity]"
meta.PropertiesKey;                   // [Id]
meta.PropertiesExceptKeyAndComputed;  // [Name, From]
```

`TypeMeta` is looked up automatically whenever a `Table<T>` is created, so in most cases you
never need to call `TypeMeta.Get<T>()` yourself:

```csharp
using Simpleverse.Repository.Db;

var identityTable = new Table<Identity>("I"); // "I" is the SQL alias used in generated queries
```

### Repository / connection setup

`DbRepository` wraps a `Func<DbConnection>` factory so that a fresh connection is opened per
operation:

```csharp
using Simpleverse.Repository.Db;
using Simpleverse.Repository.Db.SqlServer;
using Microsoft.Data.SqlClient;

var repository = new SqlRepository(() => new SqlConnection(connectionString));
```

### Defining an Entity

`Entity<TModel, TFilter, TOptions>` (or the 4-generic overload with a separate update model)
implements list/get/exists/add/update/upsert/delete against a `Table<T>`.

For a quick start, declaring a dedicated entity class is not necessary – the simplest form
is to instantiate `Entity<T>` directly, passing the table name as a plain string:

```csharp
using Simpleverse.Repository.Db;
using Simpleverse.Repository.Db.Entity;

var entity = new Entity<Identity>(repository, "I");
```

Internally this constructs a `Table<Identity>` for you. The more complicated
`(repository, Table<TModel>)` overload is still available and useful for advanced cases –
e.g. when you need to control schema qualification, quoting, or reuse a `Table<T>` instance
that already exists elsewhere:

```csharp
using Simpleverse.Repository.Db;
using Simpleverse.Repository.Db.Entity;

var entity = new Entity<Identity>(repository, new Table<Identity>("I"));
```

A dedicated class such as `IdentityEntity` below is purely a convenience once type names
start getting long (e.g. `Entity<Identity, IdentityQueryFilter, DbQueryOptions>` once a
custom filter is introduced), or once behaviour needs to be extended beyond what the base
`Entity` class offers:

```csharp
using Simpleverse.Repository.Db;
using Simpleverse.Repository.Db.Entity;

public class IdentityEntity : Entity<Identity, DbQueryOptions>
{
    public IdentityEntity(DbRepository repository)
        : base(repository, new Table<Identity>("I"))
    {
    }
}
```

A separate `TFilter` class with a `Filter` override is only needed when you want to
change or expand filtering beyond the basic columns (e.g. custom joins or filters that
don't map 1:1 to a model property):

```csharp
using Simpleverse.Repository.Db;
using Simpleverse.Repository.Db.Entity;

public class IdentityQueryFilter
{
    public string Name { get; set; }
}

public class IdentityEntity : Entity<Identity, IdentityQueryFilter, DbQueryOptions>
{
    public IdentityEntity(DbRepository repository)
        : base(repository, new Table<Identity>("I"))
    {
    }

    protected override void Filter(QueryBuilder<Identity> builder, IdentityQueryFilter filter)
    {
        builder.Where(x => x.Name, filter.Name);
        base.Filter(builder, filter);
    }
}
```

## Common scenarios

### ListAsync

`ListAsync` returns all rows matching an (optional) filter. It supports both action-based
setup and pre-built filter/options instances, and lets you project onto a different type
(including tuples for joined queries):

```csharp
var entity = new IdentityEntity(repository);

// list all
IEnumerable<Identity> all = await entity.ListAsync();

// filter with an inline setup action
IEnumerable<Identity> byName = await entity.ListAsync(
    filterSetup: filter => filter.Name = "John"
);

// project to a different shape
IEnumerable<string> names = await entity.ListAsync<string>(
    filterSetup: filter => filter.Name = "John"
);

// project a joined query onto a tuple
IEnumerable<(Identity identity, ExplicitKey explicitKey)> joined =
    await entity.ListAsync<(Identity identity, ExplicitKey explicitKey)>();
```

Cancellation is supported everywhere via the trailing `CancellationToken` parameter.

### GetAsync / ExistsAsync

```csharp
var byId = await entity.GetAsync(1); // by primary key
var byFilter = await entity.GetAsync(filter => filter.Name = "John");
var exists = await entity.ExistsAsync(filter => filter.Name = "John");
```

### Add / Update / Upsert / Delete

```csharp
await entity.AddAsync(new Identity { Name = "Jane" });

await entity.UpdateAsync(
    filter => filter.Name = "Jane",
    options => options.Set(x => x.From, "referral")
);

await entity.UpsertAsync(new Identity { Name = "Jane" });

await entity.DeleteAsync(filter => filter.Name = "Jane");
```

## Further reading

The test suite under `src/Simpleverse.Repository.Db.Test` contains runnable, end-to-end
examples for every operation (bulk insert/update/delete, merge/upsert, application locks,
change tracking, and more) and is a good reference for advanced scenarios not covered here.
