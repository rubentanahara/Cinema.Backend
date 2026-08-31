# GraphQL

One schema, one endpoint, one process. There is no federation, no gateway and no subgraph composition;
the modular monolith replaced all three. `src/Api/schema.graphql` is the checked-in snapshot.

- **GraphQL at the client edge only.** Modules talk to each other through contracts and ordinary method
  calls, never by querying their own graph.
- **Every request validates its JWT** against Cognito's JWKS.

## Registering types

The `HotChocolate.Types.Analyzers` source generator finds `[QueryType]`, `[MutationType]` and
`[ObjectType<T>]` classes at compile time and emits one registration method per assembly, named after
that assembly's `[assembly: Module(...)]` attribute. It only runs if the analyzer package is
referenced, and a decorated class registers nothing until its generated method is called.

```csharp
// src/Modules/Catalog/Properties/ModuleInfo.cs
[assembly: Module("CatalogTypes")]
```

```csharp
// src/Modules/Catalog/Graph/CatalogQueries.cs
[QueryType]
public static partial class CatalogQueries
{
    [UseFiltering]
    [UseSorting]
    public static async Task<IReadOnlyList<Movie>> GetMoviesAsync(
        QueryContext<Movie> query,
        CatalogDbContext dbContext,
        CancellationToken cancellationToken)
        => await dbContext.Movies.With(query).ToListAsync(cancellationToken);
}
```

```csharp
// src/Api/Program.cs
builder.AddGraphQL()
    .RegisterDbContextFactory<CatalogDbContext>()
    .AddFiltering()
    .AddSorting()
    .AddCatalogTypes();
```

Every module needs a distinct `Module(...)` name. Two modules both declaring `Module("Types")` emit two
`AddTypes()` methods and collide at the call site.

Do not reach for `AddGraphQLServer()` and hand-register types. That is the pre-16 shape and it bypasses
the generator.

## Reads and writes are not symmetric

**Reads go resolver to `IQueryable`, with no repository.** `QueryContext<T>` lives in `GreenDonut.Data`,
not `HotChocolate.Data`, and `.With(query)` composes filtering, sorting and projection onto the query.
The catalog query above emits exactly this against Postgres:

```sql
SELECT m."Title" FROM catalog."Movies" AS m WHERE m."RuntimeMinutes" > @p ORDER BY m."Title"
```

One column, filter and sort both pushed into SQL. A repository returning `List<Movie>` would fetch every
column and sort in memory, so the repository is not neutral ceremony on the read path, it is a
performance bug.

Never `[UseProjection]`: it is the pre-16 shape, and combining it with `QueryContext<T>` raises analyzer
**HC0099**, which `TreatWarningsAsErrors` turns into a build failure. `AddFiltering()` and `AddSorting()`
must be registered for `QueryContext<T>` to work at all.

**Writes go resolver to a command handler to the aggregate.** Invariants, domain events and the outbox
row belong together in one transaction, and that is where the ceremony earns its place.

Resolvers run in parallel and `DbContext` is not thread-safe, so a module registers
`AddDbContextFactory<T>` and the schema registers `RegisterDbContextFactory<T>`.

## Schema ownership

A field belongs to the module that owns its data. A resolver that needs another module's data goes
through that module's contract, not its `DbContext`.

| Type | Owner |
|---|---|
| `Movie`, `Cinema`, `Showtime` | catalog |
| `Order` | ordering |
| `User` | identity |
| `Ticket` | ticketing |

- **Seat map layout lives in `seating`**, denormalized from an `AuditoriumLayoutChanged` event.
- **Batch every cross-module read behind a DataLoader.** A 40-showtime list resolves prices in one call
  into `pricing`, not forty. In-process makes N+1 cheap enough to hide in development and expensive
  enough to hurt under load.
- **Relay Global Object Identification is mandatory** (`AddGlobalObjectIdentification()`), with ids
  unique across modules. The client's normalized store needs it and retrofitting is expensive.

## Changing the schema

```sh
make schema   # dotnet run -- schema export, no server needed
```

Commit the regenerated `src/Api/schema.graphql`. A breaking change then shows up as a git diff, which
is the breaking-change gate that composition used to provide.

Expand/contract still applies: the deployed API and a released mobile client are always two versions.

## Before public exposure

Query cost and depth limits (Hot Chocolate cost analysis) and a persisted-operation allowlist are both
mandatory. Without cost analysis a single deeply nested query is a denial of service.
