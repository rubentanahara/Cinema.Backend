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
make format    # dotnet format
make status    # query every module's status field through one endpoint
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
src/ServiceDefaults  OpenTelemetry, health checks, HTTP resilience
src/SharedKernel     Entity, IDomainEvent
src/Modules/*        the ten modules, one assembly each
tests/Architecture   module boundary rules
requests/api.http    status query and health probes
```

One process on port 5100, set in `src/Api/Properties/launchSettings.json`.

## Modules

A module is its own assembly. That is the whole point of the layout: `internal` is a real boundary
between modules, enforced by the compiler rather than by discipline. No module project references
another module project.

`tests/Architecture` asserts that no module assembly depends on another. It is not decoration — a
boundary with no test is a folder name. If you add a legitimate cross-module dependency, it goes
through a contract that both sides reference, never a direct project reference.

Hot Chocolate's source generator names its registration method after the module's
`[assembly: Module("<Name>Types")]` attribute, so `src/Api/Program.cs` chains `AddCatalogTypes()`
through `AddNotificationsTypes()`. Two modules declaring the same `Module(...)` name will collide.

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
@.claude/rules/service-conventions.md
