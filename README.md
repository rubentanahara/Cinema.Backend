# Cinema.Backend

Cinema ticketing platform — ten microservices behind a federated GraphQL gateway, orchestrated
locally by Aspire and deployed to AWS on demand.

The mobile client lives in [Cinema.Maui](https://github.com/rubentanahara/Cinema.Maui).
Architecture decisions are recorded in [docs/architecture-decisions.md](docs/architecture-decisions.md).

## Requirements

- .NET SDK 10.0.302 (pinned in `global.json`)
- Docker or Podman (Aspire starts a PostgreSQL container)

## Running

```sh
make run          # aspire run — dashboard, Postgres, ten services
make dev          # same, via dotnet run (no aspire CLI needed)
```

The Aspire dashboard prints a login URL on startup. It runs one PostgreSQL container with a database
per service, then starts all ten services with connection strings, OpenTelemetry, health checks and
service discovery already wired.

```sh
make              # build the whole solution
make status       # GraphQL serviceStatus on every service
make health       # /health on every service
make down         # stop the AppHost and its containers
make tools        # once: installs husky, which the git hooks invoke
```

Services are pinned to ports **5101-5110** in `AppHost.cs`, in the order listed under Structure, so the
files in `requests/` stay valid across runs.

Build output goes to `artifacts/` (`UseArtifactsOutput`), not per-project `bin/obj`.

## Structure

```
src/
  AppHost/          Aspire orchestration — PostgreSQL + the ten services
  ServiceDefaults/  OpenTelemetry, health checks, resilience, service discovery
  SharedKernel/     Entity, IDomainEvent
  Services/
    Catalog/        films, cinemas, auditoriums, showtimes
    Seating/        per-showtime seat inventory and holds
    Pricing/        price cards, quotes, fees, tax
    Ordering/       orders and the booking saga
    Payments/       authorization, capture, refunds
    Ticketing/      issuance, QR, door redemption
    Loyalty/        membership, points ledger, passes
    Concessions/    per-cinema menus
    Identity/       profile and preferences
    Notifications/  worker, no public schema
```

Each service owns its own database and exposes a GraphQL subgraph at `/graphql`, plus `/health` and
`/alive`. No service reads another's database; cross-service data is duplicated by event.

## Build gates

`TreatWarningsAsErrors` and `EnforceCodeStyleInBuild` are on, with StyleCop.Analyzers and
SonarAnalyzer.CSharp applied to every project. A style complaint, an analyzer complaint, or a known
package vulnerability (`NU1902`) fails the build. Run `dotnet build Cinema.slnx` before claiming a
change works.

Package versions are managed centrally: add a `PackageVersion` to `Directory.Packages.props` and a
`PackageReference` without a `Version` in the csproj.

Git hooks (`.husky/`): pre-commit runs `dotnet format` over staged `.cs` files and `gitleaks protect`;
commit-msg enforces Conventional Commits with a subject of 1-88 characters.

## Adding a GraphQL type

Hot Chocolate's source generator emits a registration method per assembly; it must be called
explicitly. A `[QueryType]` class alone does nothing.

```csharp
// Query.cs
[QueryType]
public static partial class ServiceQueries
{
    public static ServiceStatus GetServiceStatus() => new("catalog", DateTimeOffset.UtcNow);
}

// Program.cs
builder.Services
    .AddGraphQLServer()
    .AddCatalogTypes();   // generated from the root namespace
```

`HotChocolate.Types.Analyzers` must be referenced as an analyzer for the generator to run.
