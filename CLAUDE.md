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
make up        # docker compose: PostgreSQL on :5432
make dev       # run the API on http://localhost:5100
make           # build the whole solution
make test      # unit and architecture tests
make schema    # export the GraphQL schema to src/Api/schema.graphql
make migrate   # apply migrations; MODULE=Catalog by default
make migration MODULE=Catalog NAME=AddMovie
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

Only `catalog` has a domain. The other nine are empty assemblies holding a place in the
solution; give a module a `DbContext` when it gets its first entity, not before.

**Those nine contain no `.cs` files at all, deliberately.** An empty class library builds fine. Adding a
marker or placeholder class fails the build twice over: StyleCop `SA1649` wants the file name to match
the type, and Sonar `S2094` rejects empty classes.

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
| `GET /health` | JSON: overall status plus one entry per module |
| `GET /alive` | `Healthy` as plain text, liveness only |

```json
{ "status": "Healthy",
  "modules": { "self":    { "status": "Healthy", "description": null },
               "catalog": { "status": "Healthy", "description": null } } }
```

A module reports `Degraded` with `"3 pending migrations"` when it is behind. `/health` maps in **every**
environment: mapping it only in Development would make the ALB health check 404 and cycle the Fargate
task. Because it exposes module names and migration counts, keep it off the public listener and point
the load balancer at `/alive`.

## Database

Compose runs one PostgreSQL 18.3 with database `cinema`, user and password both `cinema`, on 5432. The
connection string lives under `ConnectionStrings:cinema` in `src/Api/appsettings.json`.

The volume mounts at `/var/lib/postgresql`, **not** `/var/lib/postgresql/data`. PostgreSQL 18 images
store data in a major-version subdirectory and refuse to start against the old path.

One database, one schema per module, each with its own `__EFMigrationsHistory` inside that schema.

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
6. `make migration MODULE=<Name> NAME=Initial<Name>` then `make migrate MODULE=<Name>`.
7. `make schema` and commit the regenerated `src/Api/schema.graphql`.

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
