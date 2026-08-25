# CLAUDE.md

Guidance for Claude Code (claude.ai/code) when working in this repository.

## Project

Cinema ticketing platform: ten .NET 10 microservices, each owning its own database and exposing a
GraphQL subgraph, composed by a Hot Chocolate Fusion gateway. Aspire orchestrates the system locally;
AWS is deployed on demand with CDK. The MAUI client is a separate repository, `Cinema.Maui`.

Architecture decisions live in `Docs/architecture-decisions.md`. Read it before proposing a structural
change — most of them are already settled there, with the reasoning.

## Commands

```sh
make run       # aspire run: Postgres + ten services + dashboard
make dev       # same, via dotnet run
make           # build the whole solution
make status    # GraphQL serviceStatus on all ten services
make health    # /health on all ten services
make down      # stop the AppHost and its containers
make tools     # once: installs husky, which the git hooks invoke
```

Prefer the Makefile over raw `dotnet` invocations; it carries the solution path and the port list.

The solution uses the XML `.slnx` format, so pass `Cinema.slnx` explicitly to `dotnet` commands that
need a solution. Build output goes to `artifacts/`, not per-project `bin/obj`.

## Structure

Folder names are Sentence case. `Src/`, `Docs/`, `Requests/`, `Src/Services/Catalog/`.

```
Src/AppHost           Aspire orchestration
Src/ServiceDefaults   OpenTelemetry, health checks, resilience, service discovery
Src/SharedKernel      Entity, IDomainEvent
Src/Services/*        the ten services
Requests/*.http       one file per service, ports 5101-5110
```

Services are pinned to fixed ports in `AppHost.cs` via `.WithEndpoint("http", e => e.Port = N)` so the
`.http` files stay valid. Without pinning, Aspire assigns random ports on every run.

## Rules

- **A service never reads another service's database.** Cross-service reads go through a contract
  interface; cross-service data is duplicated by event and stored as a snapshot.
- **GraphQL at the client edge, REST between services.** Service-to-service GraphQL couples every
  service to the gateway's type system.
- Every async method takes a `CancellationToken`.
- Return an empty collection, never null.

## Build gates

`TreatWarningsAsErrors` and `EnforceCodeStyleInBuild` are on, with StyleCop.Analyzers and
SonarAnalyzer.CSharp on every project. A style complaint, an analyzer complaint, or a known package
vulnerability (`NU1902`) fails the build. Run `dotnet build Cinema.slnx` before claiming a change works.

Package versions are managed centrally: add a `PackageVersion` to `Directory.Packages.props` and a
`PackageReference` without a `Version` in the csproj.

Git hooks (`.husky/`): pre-commit runs `dotnet format` over staged `.cs` files and `gitleaks protect`;
commit-msg enforces Conventional Commits with a subject of 1-88 characters.

## Hot Chocolate

The source generator emits one registration method per assembly and it must be called explicitly —
a `[QueryType]` class on its own registers nothing.

```csharp
[QueryType]
public static partial class ServiceQueries
{
    public static ServiceStatus GetServiceStatus() => new("catalog", DateTimeOffset.UtcNow);
}

builder.Services.AddGraphQLServer().AddCatalogTypes();
```

`HotChocolate.Types.Analyzers` must be referenced as an analyzer for the generator to run.

# AWS Guidance

- Prefer the AWS MCP Server for AWS interactions — it provides sandboxed
  execution, observability, and audit logging. If unavailable, use the
  AWS CLI directly.
- Before starting a task, check whether a relevant AWS skill is available.
  Load the skill with `retrieve_skill` and prefer its guidance over
  general knowledge.
- When uncertain about specific AWS details (API parameters, permissions,
  limits, error codes), verify against documentation rather than guessing.
  State uncertainty explicitly if you cannot confirm.
- When creating infrastructure, prefer infrastructure-as-code (AWS CDK or
  CloudFormation) over direct CLI commands.
- When working with infrastructure, follow AWS Well-Architected Framework
  principles.
- Do not use em dashes in AWS resource names or descriptions. Use
  hyphens instead.

## Secret Safety

- MUST load the `aws-secrets-manager` skill first for any secret,
  credential, API key, token, or password task. MUST NOT call
  `secretsmanager get-secret-value` or `batch-get-secret-value`, and MUST
  NOT hit the Secrets Manager Agent daemon directly. MUST use
  `{{resolve:secretsmanager:secret-id:SecretString:json-key}}` with
  `asm-exec` so the secret resolves at runtime without entering context.
