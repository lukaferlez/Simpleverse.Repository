# Simpleverse.Repository
![build](https://github.com/lukaferlez/Simpleverse.Repository/workflows/build/badge.svg)

A lightweight repository pattern built on top of [Dapper](https://github.com/DapperLib/Dapper),
providing generic query/list/add/update/upsert/delete operations for SQL Server, plus bulk
operations, application locks, and change tracking.

## Packages

- **Simpleverse.Repository** – database-agnostic abstractions (`IEntity`, `IQueryList`, `IAdd`,
  `IUpdate`, `IUpsert`, `IReplace`, `IDelete`, etc.).
- **Simpleverse.Repository.Db** – SQL Server / Dapper implementation of those abstractions.

## Install

```
dotnet add package Simpleverse.Repository.Db
```

## Quick start

```csharp
using Microsoft.Data.SqlClient;
using Simpleverse.Repository.Db;
using Simpleverse.Repository.Db.Entity;
using Simpleverse.Repository.Db.SqlServer;

// 1. Configure a repository
var repository = new SqlRepository(() => new SqlConnection(connectionString));

// 2. Map a model
[Dapper.Contrib.Extensions.Table("[Identity]")]
public class Identity
{
    [Dapper.Contrib.Extensions.Key]
    public int Id { get; set; }
    public string Name { get; set; }
}

// 3. Define an entity and query it
// A separate filter class isn't necessary for a quick start – the base Entity.Filter
// maps "virtual" model properties automatically, so the model itself can be used as
// the filter type.
public class IdentityEntity : Entity<Identity, DbQueryOptions>
{
    public IdentityEntity(DbRepository repository)
        : base(repository, new Table<Identity>("I")) { }
}

var entity = new IdentityEntity(repository);
var results = await entity.ListAsync(filter => filter.Name = "John");
```

## Documentation

See [docs/USAGE.md](docs/USAGE.md) for a full walkthrough covering entity mapping,
`TypeMeta` configuration, and common usage scenarios such as `ListAsync`, filtering, and
CRUD operations.
