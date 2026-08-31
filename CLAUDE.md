# CLAUDE.md

Guidance for Claude Code (claude.ai/code) when working in this repository.

## Project

Cinema ticketing platform: ten .NET 10 microservices, each owning its own database and exposing a
GraphQL subgraph, composed by a Hot Chocolate Fusion gateway. Postgres runs in Docker Compose; AWS is
the deployment target, not yet built. The MAUI client is a separate repository, `Cinema.Maui`.

**This shape is being collapsed to a modular monolith.** Aspire is already removed. The ten service
projects and the Fusion gateway are next; they hold 25 lines of boilerplate each and no domain logic.

Architecture decisions live in `Docs/architecture-decisions.md`. Read it before proposing a structural
change — most of them are already settled there, with the reasoning.

`graphify-out/` holds a knowledge graph of this repo. For "where does X live" or "how does Y work",
run `graphify query "<question>"` instead of reading your way across the services.

## Commands

```sh
make up        # docker compose: Postgres + the OTLP dashboard on :18888
make logs      # follow compose logs
make down      # stop the compose stack
make           # build the whole solution
make test      # dotnet test
make format    # dotnet format
make schema    # export each subgraph's SDL to Src/Services/<Service>/schema.graphql
make status    # federated status query through the gateway
make health    # gateway /health
make clean     # down, then delete artifacts/
make tools     # once: installs husky, which the git hooks invoke
```

Prefer the Makefile over raw `dotnet` invocations; it carries the solution path and the gateway URL.

The solution uses the XML `.slnx` format, so pass `Cinema.slnx` explicitly to `dotnet` commands that
need a solution. Build output goes to `artifacts/`, not per-project `bin/obj`.

## Structure

Folder names are Sentence case — `Src/`, `Docs/`, `Requests/`, `Src/Services/Catalog/` — except tooling
directories that must keep their own names (`.husky`, `.config`, `artifacts`). Every path in
`Cinema.slnx`, `Makefile`, `aspire.config.json` and `Scripts/` is case-exact; macOS will not tell you
when it drifts, Linux CI will.

```
Src/Gateway           Fusion gateway, port 5100, loads gateway.far
Src/ServiceDefaults   OpenTelemetry, health checks, resilience, service discovery
Src/SharedKernel      Entity, IDomainEvent
Src/Services/*        the ten services
Requests/gateway.http federated query and health probes
Scripts/              export-schemas.sh
```

There is no orchestrator. Each project runs standalone on its `Properties/launchSettings.json` port —
gateway 5098, Catalog 5203, and so on — so nothing coordinates startup order or hands out connection
strings. `Requests/gateway.http` still points at 5100 and will not resolve until the collapse lands.

Telemetry goes to the standalone dashboard container over OTLP. Nothing exports unless
`OTEL_EXPORTER_OTLP_ENDPOINT` is set, so the app runs fine with the stack down.

## Build gates

`TreatWarningsAsErrors` and `EnforceCodeStyleInBuild` are on, with StyleCop.Analyzers and
SonarAnalyzer.CSharp on every project. A style complaint, an analyzer complaint, or a known package
vulnerability (`NU1902`) fails the build. Run `dotnet build Cinema.slnx` before claiming a change works.

Package versions are managed centrally: add a `PackageVersion` to `Directory.Packages.props` and a
`PackageReference` without a `Version` in the csproj.

Git hooks (`.husky/`): pre-commit runs `dotnet format` over staged `.cs` files and `gitleaks protect`;
commit-msg enforces Conventional Commits with a subject of 1-88 characters.

## Rules

Read these before adding a service, a schema type, an event, or a saga step.

@.claude/rules/service-boundaries.md
@.claude/rules/graphql-federation.md
@.claude/rules/service-conventions.md
