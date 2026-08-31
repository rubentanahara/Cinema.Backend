# Cinema.Backend

Cinema ticketing platform: a .NET 10 modular monolith. Ten modules, each its own assembly with an
enforced boundary, composed by one host exposing a single GraphQL schema.

The mobile client lives in [Cinema.Maui](https://github.com/rubentanahara/Cinema.Maui).
Architecture decisions are recorded in [docs/architecture-decisions.md](docs/architecture-decisions.md).

## Requirements

- .NET SDK 10.0.302 (pinned in `global.json`)
- Docker or Podman (Compose runs PostgreSQL)

## Running

```sh
make up           # docker compose: PostgreSQL on 5432
make dev          # run the API on http://localhost:5100
```

```sh
make              # build the whole solution
make test         # unit and architecture tests
make schema       # export the GraphQL schema to src/Api/schema.graphql
make migrate      # apply migrations for MODULE (default Catalog)
make status       # query every module's status field through one endpoint
make health       # /health
make down         # stop the compose stack
make tools        # once: installs husky, which the git hooks invoke
```

Build output goes to `artifacts/` (`UseArtifactsOutput`), not per-project `bin/obj`.

## Structure

```
src/
  Api/              the host: GraphQL endpoint, health checks, module registration
  ServiceDefaults/  OpenTelemetry, health checks, HTTP resilience
  SharedKernel/     Entity, IDomainEvent
  Modules/
    Catalog/        films, cinemas, auditoriums, showtimes
    Seating/        per-showtime seat inventory and holds
    Pricing/        price cards, quotes, fees, tax
    Ordering/       orders and the booking saga
    Payments/       authorization, capture, refunds
    Ticketing/      issuance, QR, door redemption
    Loyalty/        membership, points ledger, passes
    Concessions/    per-cinema menus
    Identity/       profile and preferences
    Notifications/  outbound mail and push
tests/
  Architecture/     module boundary rules
  Catalog/          integration tests against a real Postgres container
```

Only `catalog` has a domain so far. The other nine are empty assemblies holding their place.

One process, one database, one schema per module. A module is its own assembly, so `internal` is a
real boundary: `Cinema.Ordering` cannot see `Cinema.Catalog`'s internals because the compiler will not
allow it. No module project references another module project, and
`tests/Architecture` fails the build if one starts to.

Cross-module data is duplicated by event and stored as a snapshot, never read across a boundary.

## Build gates

`TreatWarningsAsErrors` and `EnforceCodeStyleInBuild` are on, with StyleCop.Analyzers and
SonarAnalyzer.CSharp applied to every project. A style complaint, an analyzer complaint, or a known
package vulnerability (`NU1902`) fails the build. Run `make` before claiming a change works.

Package versions are managed centrally: add a `PackageVersion` to `Directory.Packages.props` and a
`PackageReference` without a `Version` in the csproj.

## Adding a GraphQL type

Hot Chocolate's source generator emits one registration method per assembly, named after the module's
`[assembly: Module(...)]` attribute. It must be called explicitly; a `[QueryType]` class alone does
nothing.

```csharp
// src/Modules/Catalog/Properties/ModuleInfo.cs
[assembly: Module("CatalogTypes")]

// src/Modules/Catalog/Types/Query.cs
[QueryType]
public static partial class CatalogQueries
{
    public static CatalogStatus GetCatalogStatus() => new("catalog", DateTimeOffset.UtcNow);
}
```

```csharp
// src/Api/Program.cs
builder.AddGraphQL()
    .AddCatalogTypes();
```

`HotChocolate.Types.Analyzers` must be referenced as an analyzer for the generator to run.

## Containers

There is no Dockerfile. The SDK builds the image:

```sh
dotnet publish src/Api/Cinema.Api.csproj -c Release --os linux --arch arm64 /t:PublishContainer
```
