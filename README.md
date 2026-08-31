# Cinema.Backend

Cinema ticketing platform: a .NET 10 modular monolith. Ten modules, each its own assembly with an
enforced boundary, composed by one host exposing a single GraphQL schema.

The mobile client lives in [Cinema.Maui](https://github.com/rubentanahara/Cinema.Maui).
Architecture decisions are recorded in [docs/architecture-decisions.md](docs/architecture-decisions.md).
The current architecture is drawn in [docs/Diagrams/system-architecture.html](docs/Diagrams/system-architecture.html).

## Requirements

- .NET SDK 10.0.302 (pinned in `global.json`)
- Docker or Podman (Compose runs PostgreSQL)

## Running

```sh
make tools        # once: dotnet-ef and husky into the local tool manifest
make up           # build the API image, then start PostgreSQL and the API
make migrate      # create all ten module schemas
make seed         # three sample movies
```

The API is on http://localhost:5100. `make dev` runs it as a host process instead, for a faster inner
loop than rebuilding the image.

```sh
make              # build the whole solution
make test         # unit and architecture tests
make schema       # export the GraphQL schema to src/Api/schema.graphql
make migrate      # apply every module's migrations, in MODULES order
make migration MODULE=Catalog NAME=AddMovie   # scaffold one, NAME required
make seed         # idempotent sample data
make image        # publish cinema-api:latest via the SDK, no Dockerfile
make status       # smoke query: { movies { title } }
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

Every module owns a schema, a migration and a health check. Only `catalog` has a domain and a GraphQL
surface so far; the other nine hold an empty schema ready for their first entity.

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

`HotChocolate.Types.Analyzers` must be referenced as an analyzer for the generator to run.

## Containers

There is no Dockerfile. The SDK builds the image:

```sh
dotnet publish src/Api/Cinema.Api.csproj -c Release --os linux --arch arm64 /t:PublishContainer
```
