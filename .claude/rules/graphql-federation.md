# GraphQL and federation

- **GraphQL at the client edge, REST between services.** Service-to-service GraphQL couples every service to
  the gateway's type system.
- The Fusion gateway (`src/Gateway`, port 5100) is the only public entry point and the only gateway. No
  second routing layer.
- **Every subgraph validates the JWT itself** against Cognito's JWKS. The gateway propagates the
  `Authorization` header; it does not vouch.

## Registering types

The `HotChocolate.Types.Analyzers` source generator finds `[QueryType]`, `[MutationType]` and
`[ObjectType<T>]` classes at compile time and emits the registration. It only runs if the analyzer package is
referenced. A decorated class on its own registers nothing until `AddTypes()` is called.

```csharp
[QueryType]
public static partial class CatalogQueries
{
    public static CatalogStatus GetCatalogStatus() => new("catalog", DateTimeOffset.UtcNow);
}
```

```csharp
builder.AddGraphQL().AddTypes();
```

`AddTypes()` is the generated per-assembly method. Do not reach for `AddGraphQLServer()` and hand-register
types — that is the pre-16 shape and it bypasses the generator.

## Schema ownership

A field belongs to the service that owns its data. Extensions go the other way — the owner never learns
about its extenders.

| Type | Owner | Extended by |
|---|---|---|
| `Movie`, `Cinema`, `Showtime` | catalog | concessions, seating, pricing |
| `Order` | ordering | ticketing, payments |
| `User` | identity | ordering, loyalty |
| `Ticket` | ticketing | — |

- **Seat map layout lives in `seating`**, denormalized from an `AuditoriumLayoutChanged` event. A gateway
  join across 200 seats on the most latency-sensitive screen is the distributed-monolith answer.
- **Every `@lookup` is batched.** A 40-showtime list resolves prices in one call to `pricing` with 40 keys.
  Unbatched lookups stay invisible until the list gets long, then they kill the graph.
- **Relay Global Object Identification is mandatory in every subgraph**
  (`AddGlobalObjectIdentification()`), with ids unique across services. Retrofitting it into ten schemas
  later is expensive.

## Changing a schema

Composition is wired through a checked-in `gateway.far` loaded by `AddFileSystemConfiguration`. Editing a
subgraph's types is not enough — the composed artifact has to be rebuilt or the gateway serves the old graph.

```sh
make schema   # runs each subgraph, writes src/Services/<Service>/schema.graphql
```

Then recompose `gateway.far` from those SDL files. Schema composition failure is the breaking-change gate:
if it fails, the change breaks a consumer, so fix the change rather than the gate.

Expand/contract for every schema change. Services deploy independently, so old and new run simultaneously
by definition.

## Before public exposure

Query cost and depth limits (Hot Chocolate cost analysis) and a persisted-operation allowlist are both
mandatory. Without cost analysis a single deeply nested query is a denial of service.
