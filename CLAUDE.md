# CLAUDE.md

Guidance for Claude Code (claude.ai/code) when working in this repository.

## Project

Cinema ticketing platform: a .NET 10 modular monolith. Ten modules, each its own assembly, composed by
one host (`src/Api`) exposing a single GraphQL schema. PostgreSQL runs in Docker Compose. AWS is the
deployment target, not yet built. The MAUI client is a separate repository, `Cinema.Maui`.

Architecture decisions live in `docs/architecture-decisions.md`. Read it before proposing a structural
change — most of them are already settled there, with the reasoning.

`graphify-out/` holds a knowledge graph of this repo. For "where does X live" or "how does Y work",
run `graphify query "<question>"` instead of reading your way across the modules.

## Commands

```sh
make up        # build the API image, then start PostgreSQL and the API
make dev       # run the API as a host process instead, faster inner loop
make image     # publish cinema-api:latest via the SDK, no Dockerfile
make           # build the whole solution
make test      # unit and architecture tests
make schema    # export the GraphQL schema to src/Api/schema.graphql
make migrate   # apply migrations for all ten modules in order
make migration MODULE=Catalog NAME=AddMovie
make seed      # three sample movies, idempotent
make format    # dotnet format
make status    # smoke query: { movies { title } }
make health    # /health
make down      # stop the compose stack
make clean     # down, then delete artifacts/
make tools     # once: installs husky, which the git hooks invoke
```

Prefer the Makefile over raw `dotnet` invocations; it carries the solution path, the host project and
the API URL.

The solution uses the XML `.slnx` format, so pass `Cinema.slnx` explicitly to `dotnet` commands that
need a solution. Build output goes to `artifacts/`, not per-project `bin/obj`.

## Running locally

Everything runs in Docker. Needs the .NET 10 SDK (pinned in `global.json`) to build the image and run
migrations, plus a running Docker daemon. From a fresh clone:

```sh
make tools     # installs dotnet-ef and husky into the local tool manifest
make up        # builds the API image, then starts PostgreSQL and the API
make migrate   # creates the catalog schema on a virgin database
make seed      # three movies, so the API returns something
```

`make up` depends on `make image`, so it republishes the container every time. There is no Dockerfile:
`dotnet publish /t:PublishContainer` produces `cinema-api:latest` and Compose consumes that tag.

`make tools` is not optional before `make migrate`. `dotnet-ef` is a **local** tool pinned at 10.0.11 in
`.config/dotnet-tools.json`; without a restore you either get "command not found" or a globally installed
older version running against EF Core 10 packages.

Nothing migrates on startup and nothing seeds automatically, so the API comes up before its schema
exists and says so:

```sh
curl -s localhost:5100/health   # before migrate
{"status":"Degraded","modules":{"catalog":{"status":"Degraded","description":"1 pending migrations"}}}
```

Migrating on startup is deliberately avoided: replicas race, and a failed migration should not take the
app down with it.

```sh
make health    # {"status":"Healthy",...}
make status    # {"data":{"movies":[{"title":"Dune"},...]}}
make logs      # follow both containers
make down      # stop the stack
```

**`make dev` runs the API as a host process instead**, for a faster inner loop than rebuilding the
image. It reads `ConnectionStrings:cinema` from `src/Api/appsettings.json`, which points at
`localhost:5432`. Stop the containerised API first or the two fight over port 5100.

`make test` needs Docker too. The integration tests start their own throwaway PostgreSQL through
Testcontainers and never touch the Compose database.

To start over, `docker compose down -v` drops the volume. If you ever delete rows from
`__EFMigrationsHistory` by hand, `make migrate` then fails with `42P07 relation already exists`, because
the tables survive but EF believes nothing is applied. Recover with `drop schema catalog cascade`, then
`make migrate && make seed`.

## Structure

Top-level folders are lowercase (`src/`, `docs/`, `requests/`); directories inside `src/` are Sentence
case.

```
src/Api              host: GraphQL endpoint, health checks, module registration
src/ServiceDefaults  OpenTelemetry, health checks, HTTP resilience, ModuleHealthCheck<T>
src/SharedKernel     Entity, IDomainEvent
src/Modules/*        the ten modules, one assembly each
tests/Architecture   module boundary rules
tests/Catalog        integration tests against a real Postgres container
requests/api.http    query and health probes
```

One process on port 5100, set in `src/Api/Properties/launchSettings.json`.

All ten modules have a `DbContext`, a schema, a migration and a health check. Only `catalog` has a
domain and a GraphQL surface; the other nine own an empty schema waiting for their first entity.

Each module's initial migration is a single `EnsureSchema`, which is idempotent, so re-running
`make migrate` over an existing database is safe. Their `Down` is deliberately empty with an `S1186`
comment: the schema holds that module's `__EFMigrationsHistory`, so dropping it would erase the record
of the rollback itself.

`src/Api/Program.cs` ends with a `public partial class Program` carrying a `protected` constructor. That
exists so `WebApplicationFactory<Program>` can reach it from the test project; the constructor is there
because Sonar `S1118` rejects a public class with only static members.

A module's layout, once it has code:

```
src/Modules/Catalog/
  CatalogModule.cs        public: AddCatalog registers the context and its health check
  Domain/                 entities on the GraphQL surface are public, everything else internal
  Infrastructure/         CatalogDbContext, Migrations/
  Graph/                  CatalogQueries
```

## Modules

A module is its own assembly. That is the whole point of the layout: `internal` is a real boundary
between modules, enforced by the compiler rather than by discipline. No module project references
another module project.

`tests/Architecture` asserts that no module assembly depends on another. It is not decoration — a
boundary with no test is a folder name. If you add a legitimate cross-module dependency, it goes
through a contract that both sides reference, never a direct project reference.

Hot Chocolate's source generator names its registration method after the module's
`[assembly: Module("<Name>Types")]` attribute, so `src/Api/Program.cs` calls `AddCatalogTypes()`. Two
modules declaring the same `Module(...)` name will collide.

Reads use `QueryContext<T>` from `GreenDonut.Data` with `.With(query)`, never `[UseProjection]`: mixing
them raises analyzer HC0099, which `TreatWarningsAsErrors` turns into a build failure. `AddFiltering()`
and `AddSorting()` must be registered for `QueryContext<T>` to work.

Modules register `AddDbContextFactory<T>`, not a scoped `DbContext`, because resolvers run in parallel.
Each pins its own `MigrationsHistoryTable("__EFMigrationsHistory", "<schema>")` or ten modules fight over
one table.

## Endpoints

| Endpoint | Returns |
|---|---|
| `POST /graphql` | the only data endpoint |
| `GET /graphql` | 301 to the Nitro IDE |
| `GET /health` | JSON: overall status plus one entry per registered check |
| `GET /alive` | `Healthy` as plain text, liveness only |

The container listens on 8080 and Compose publishes it as 5100, so the port is 5100 either way.

```json
{ "status": "Healthy",
  "modules": { "self":    { "status": "Healthy", "description": null },
               "catalog": { "status": "Healthy", "description": null } } }
```

Eleven entries: `self` from `AddDefaultHealthChecks()` in `ServiceDefaults`, plus one per module from
`AddModuleCheck<TContext>(Schema)` in each `<Name>Module.cs`. They are independent, not ten copies of one
probe: clearing `seating.__EFMigrationsHistory` degrades `seating` alone and leaves the other ten
healthy.

A module behind on migrations reports `Degraded`:

```json
{ "status": "Degraded",
  "modules": { "catalog": { "status": "Degraded", "description": "1 pending migrations" } } }
```

**`Degraded` still returns HTTP 200.** ASP.NET Core only sends 503 for `Unhealthy`, so a load balancer
keeps a task in service while its migrations are pending. That is defensible, since the app does serve
traffic, but a 200 from `/health` does not mean fully ready. Read the body, not the status code.

`/health` maps in **every** environment: mapping it only in Development would make the ALB health check
404 and cycle the Fargate task. Because it exposes module names and migration counts, keep it off the
public listener and point the load balancer at `/alive`.

## Database

Compose runs one PostgreSQL 18.3 with database `cinema`, user and password both `cinema`, on 5432.

There are two connection strings for the same database, because the caller's network differs:

| Caller | Host | Set in |
|---|---|---|
| API container | `postgres` | `ConnectionStrings__cinema` env var in `compose.yaml` |
| `make dev`, `make migrate`, `dotnet ef` | `localhost` | `ConnectionStrings:cinema` in `src/Api/appsettings.json` |

The container reaches Postgres by Compose service name; host tools go through the published port. The
env var uses `__` because that is how .NET maps environment variables onto configuration sections.

The volume mounts at `/var/lib/postgresql`, **not** `/var/lib/postgresql/data`. PostgreSQL 18 images
store data in a major-version subdirectory and refuse to start against the old path.

One database, ten schemas, each with its own `__EFMigrationsHistory` inside that schema. Verified: eleven
namespaces in `pg_namespace` (ten modules plus `public`), and ten history tables one per schema.

## Adding a module

`catalog` is the reference; copy its shape.

1. `Properties/ModuleInfo.cs` with `[assembly: Module("<Name>Types")]`, distinct from every other module.
2. `Domain/`, `Infrastructure/<Name>DbContext.cs`, `Graph/<Name>Queries.cs`.
3. `<Name>Module.cs` exposing `Add<Name>(this IHostApplicationBuilder)`, registering
   `AddDbContextFactory<T>` and `AddModuleCheck<T>(Schema)`.
4. Add the package references catalog has: `HotChocolate.Data`, `HotChocolate.Data.EntityFramework`,
   `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.EntityFrameworkCore.Design`, plus project
   references to `SharedKernel` and `ServiceDefaults`.
5. Wire it in `src/Api/Program.cs`: `builder.Add<Name>()` and `.Add<Name>Types()`.
6. Add it to `MODULES` in the Makefile, then `make migration MODULE=<Name> NAME=Initial<Name>` and
   `make migrate`.
7. `make schema` and commit the regenerated `src/Api/schema.graphql`.

With ten `DbContext`s in the solution, every `dotnet ef` command needs `--context <Name>DbContext` or it
fails with "More than one DbContext was found". The Makefile targets pass it for you.

A `DbContext` with no entities generates a migration with empty `Up` and `Down`, which fails the build on
Sonar `S1186` and would not create the schema either. Replace the `Up` body with
`migrationBuilder.EnsureSchema(name: "<schema>")`.

## Build gates

`TreatWarningsAsErrors` and `EnforceCodeStyleInBuild` are on, with StyleCop.Analyzers and
SonarAnalyzer.CSharp on every project. A style complaint, an analyzer complaint, or a known package
vulnerability (`NU1902`) fails the build. Run `make` before claiming a change works.

Package versions are managed centrally: add a `PackageVersion` to `Directory.Packages.props` and a
`PackageReference` without a `Version` in the csproj.

Git hooks (`.husky/`): pre-commit runs `dotnet format` over staged `.cs` files and `gitleaks protect`;
commit-msg enforces Conventional Commits with a subject of 1-88 characters.

## Containers

There is no Dockerfile and none is wanted. The SDK builds the image:

```sh
dotnet publish src/Api/Cinema.Api.csproj -c Release --os linux --arch arm64 /t:PublishContainer
```

## Rules

Read these before adding a module, a schema type, an event, or a saga step.

@.claude/rules/module-boundaries.md
@.claude/rules/graphql.md
@.claude/rules/conventions.md
