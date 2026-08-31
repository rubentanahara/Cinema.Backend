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
[QueryType]
public static partial class CatalogQueries
{
    public static CatalogStatus GetCatalogStatus() => new("catalog", DateTimeOffset.UtcNow);
}
```

```csharp
// src/Api/Program.cs
builder.AddGraphQL()
    .AddCatalogTypes()
    .AddSeatingTypes();
```

Every module needs a distinct `Module(...)` name. Two modules both declaring `Module("Types")` emit two
`AddTypes()` methods and collide at the call site.

Do not reach for `AddGraphQLServer()` and hand-register types. That is the pre-16 shape and it bypasses
the generator.

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
