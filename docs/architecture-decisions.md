# Cinema — Architecture Decision Log

Interim record of decisions taken in the architecture session. Feeds the formal ADRs (MADR,
one file per decision), the domain model document, and the per-phase PRDs.

Repositories: `rubentanahara/Cinema.Backend` (modules, API, infra), `rubentanahara/Cinema.Maui` (mobile client).
Design reference: `CinepolisUI/` — 39-screen critique of Cinépolis GO, 80 Critical / 87 Important findings.
The app is a corrected redesign of that product, not a clone. Defect IDs (`S1`–`S7`, `B1`–`B8`) become
acceptance criteria in the phase PRDs.

## Product scope

Full product, no surface cut: booking, concessions, loyalty (Club), passes, favorites, notifications,
profile, auth. Scope is sequenced by phase, never reduced.

## Market

Single market: **United States**. USD, en-US, 6 timezones.

- `Money` is a value object carrying its currency. No bare `decimal` crosses a boundary.
- `Cinema` owns an IANA `TimeZoneId`. A showtime is an instant plus the venue's zone, never a naked `DateTime`.
- Sales tax is **exclusive** (added at checkout) and varies by state/county/city; admissions exemptions vary.
- Age ratings follow **MPA** (G/PG/PG-13/R/NC-17). R gates purchase.
- No multi-tenancy. One chain, one operator.

## Architecture

| # | Decision | Rationale |
|---|---|---|
| 1 | **Modular monolith**, not microservices | one deployable unit, module boundaries enforced by the compiler and an architecture test. The project's goal is full-stack breadth; ten services spend that budget on coordination instead |
| 2 | **10 modules**, one assembly each | granularity by transaction boundary and change cadence, not by counting nouns. Same boundaries microservices would have drawn, enforced by `internal` instead of a network hop |
| 3 | **One database, one schema per module** | `catalog.movies`, `ordering.orders`. No foreign key or join crosses a schema line, so a module stays extractable |
| 4 | **GraphQL for callers we control, REST for callers we do not** | one schema for the MAUI client. REST only where the caller has its own fixed contract (PSP webhook, door scanner) or the payload is binary. Nothing internal is HTTP: modules call each other in-process |
| 5 | **One GraphQL schema**, no federation | one process has nothing to federate. `src/Api/schema.graphql` is checked in, so a breaking change is a git diff rather than a composition step |
| 6 | **Transaction first, saga only for payments** | committing a hold, writing an order and issuing a ticket is one database transaction. The PSP is genuinely remote, so that step alone keeps explicit compensation |
| 7 | **Transactional outbox → EventBridge → SQS per consumer** + DLQ | `PaymentCaptured` must issue tickets, award points, and email a receipt even if the process dies |
| 8 | **ECS Fargate**, one task | not EKS (teaches Kubernetes, a different skill), not Lambda (`graphql-ws` needs long-lived connections; chained cold starts). App Runner is closed to new customers |
| 9 | **CDK in C#** | one language across the repo; accepts that CDK examples are TypeScript-first |
| 10 | **Monorepo for services**, separate mobile repo | independent deployability is a property of the pipeline, not the repository |

### Services

| Service | Contexts owned |
|---|---|
| `catalog` | Film Catalog, Venue, Programming |
| `seating` | Seat Inventory |
| `pricing` | Pricing |
| `ordering` | Ordering, saga orchestration |
| `payments` | Payments |
| `ticketing` | Ticketing, redemption |
| `loyalty` | Loyalty, passes |
| `concessions` | Concessions catalog |
| `identity` | Identity, profile, preferences |
| `notifications` | Notifications (worker, no public schema) |

Catalog/Venue/Programming are one service because they are always queried together on the highest-traffic
screens; splitting them buys a distributed join and nothing else.

**Back-office is not a service.** Admin operations are role-gated mutations on the service owning the data.
A back-office service writing into nine databases is a distributed monolith.

**Favorites is not a context** — it is preferences in `identity`. **Search is not a context** — it is a read
model over catalog data.

### Cross-module data

Duplicated by event, never fetched at read time. An order snapshots movie title, poster URL, cinema name,
showtime instant and seat labels at placement. Receipts must render identically years later, after a title
is delisted. `ordering` and `catalog` disagreeing briefly is correct behaviour.

Communication between modules, in order of preference:

1. **None.** The consumer already holds a snapshot, kept current by event.
2. **Integration event**, in-process, published through the outbox. The publisher does not know its
   consumers. Event types live in `Cinema.<Module>.Contracts`.
3. **A synchronous call through a contracts assembly**, only when the answer must be computed now rather
   than read from a copy: a price quote, a seat availability check.

Never a second module's `DbContext`, never a cross-schema join, never a project reference to another
module's implementation. The first two are invisible to the architecture test, which is exactly why they
are the ones requiring discipline.

Options 1 and 2 already work across a network. Only option 3 changes shape when a module is extracted,
and swapping its implementation for an HTTP client is the whole extraction.

## Seat holds

The highest-contention path in the system.

```sql
create unique index seat_held_once
  on seat_holds (showtime_id, seat_id)
  where released_at is null and expires_at > now();
```

- A hold is a **row, not a lock**. No database transaction is held open across human think-time.
- Double-sell is impossible at the storage layer, not by application discipline.
- **TTL 10 minutes**, server-authoritative, extended once on entering payment, capped at 15 total. The client
  renders a countdown from the server's `expiresAt`.
- **Expiry is a predicate** (`expires_at > now()`), not a scheduled job. A janitor sweeps released rows for
  hygiene only. Correctness never depends on a cron firing.
- Hold creation is **idempotent** on a client-supplied key.
- States: `Held → Committing → Sold`. A hold entering `Committing` stops being expirable, so a slow PSP call
  cannot race the TTL and sell a seat the user just paid for.

Rejected: `SELECT … FOR UPDATE` across the flow (exhausts the connection pool), Redis `SET NX` (two sources of
truth; a failover drops holds mid-payment), actor-per-showtime (correct, but adds a runtime for one problem
Postgres already solves).

## Realtime

**Seat map only.** One realtime surface; everything else is request/response.

`seating` owns seat events and the API serves the subscription directly over `graphql-ws` on
WebSocket. **No message broker.** With one process there is no cross-service stream to bridge, and
Redis Pub/Sub and SQS cannot do resumable streams regardless.

A second Fargate task would break this: in-memory subscription state is per-process. Scaling out means
a backplane, which is the point at which the seat map becomes the reason to extract `seating`.

## Identity

- **Cognito user pool, native SRP flows (`USER_SRP_AUTH`). No Hosted UI.** Login and signup are native MAUI
  screens. The corpus's worst trust failure (`B4`) is a hosted-webview handoff showing `appleid.apple.com`
  while rendering a signup form — adopting Hosted UI would inherit the defect being fixed.
- **The API validates every JWT itself** against Cognito's JWKS on each request. No component vouches for
  another, so there is no trusted-network assumption to violate later.
- `identity` owns profile and preferences keyed by Cognito `sub`. Cognito holds credentials only.
- **Guest checkout supported.** `Order.UserId` is nullable with a required contact email; tickets are
  delivered by QR link; a claim path attaches the purchase to an account created later.
- Machine identities (door scanner, back-office) use `client_credentials` with scopes.
- There is no internal service-to-service traffic to secure. The single task sits in a private subnet and
  the ALB is the only way in.
- Account deletion spans all 10 modules and must wipe the on-device store. Most of it is one transaction;
  the external steps (Cognito, mail provider) keep compensation.

## Edge and gateway

`Cinema.Api` is the single public entry point. The ALB terminates TLS and forwards to it; nothing
routes between components because there is only one.

| Candidate | Verdict | Reason |
|---|---|---|
| **AWS API Gateway** | rejected | GraphQL is one `POST /graphql`; all routing lives in the request body, so path/method routing has nothing to act on. Its WebSocket API terminates and reframes connections around a route-selection expression rather than passing them through, which `graphql-ws` needs. Caching and request validation are meaningless against a GraphQL document. It cannot replace the ALB either — private integration still needs VPC Link to an ALB/NLB or Cloud Map |
| **YARP** | rejected for now | there are three static routes and the ALB does them declaratively, with per-target health checks and no code to deploy. YARP earns its place with per-request transformation, dynamic destination discovery, session affinity, or strangler-fig migration, none of which apply. It is middleware, so it can be added to the API process later if a real need appears |
| **Ocelot** | rejected | a REST-oriented API gateway. Its aggregation feature solves a problem one GraphQL endpoint does not have |

### Edge topology

```
ALB (ACM TLS, optional WAF)
  /graphql       -> Cinema.Api
  /webhooks/psp  -> Cinema.Api  (inbound, signature-verified)
  /redemptions   -> Cinema.Api  (door scanner, client_credentials)
```

Three ALB listener rules, three target groups. A PSP webhook is an inbound machine callback, not a client
query, so it does not pass through the GraphQL gateway.

### API surface

One deployable unit, `Cinema.Api`, local port **5100**. The ALB is a load balancer, not a gateway, and
exists only in AWS. AWS API Gateway, YARP and Ocelot remain rejected above. There is nothing left to
route between.

`src/Api/schema.graphql` is exported by `make schema` and checked in. Reviewing its diff is the
breaking-change gate that schema composition used to provide.

| Module | GraphQL | REST |
|---|---|---|
| catalog, pricing, concessions | query | — |
| seating | query + seat-map subscription | — |
| ordering, loyalty, identity | query + mutation | — |
| payments | query + mutation | `POST /webhooks/psp` |
| ticketing | query + mutation | `POST /redemptions`, QR image |
| notifications | — | — |

REST appears exactly twice, both times because the caller is not ours: a PSP posting a signed webhook,
and door-scanner hardware holding `client_credentials`. Binary payloads are the third case, since a
GraphQL field cannot carry a QR image or a ticket PDF; those go to a plain endpoint or an S3 URL.

`notifications` has no public surface at all. It only reacts to events, and that is not a gap.

A module needing routes exposes `MapX(IEndpointRouteBuilder)` beside its `AddX`, and the host calls it
explicitly. No endpoint auto-discovery: the host stays a readable table of contents.

### Required edge hardening

| Concern | Where |
|---|---|
| **Query cost and depth limits** | Hot Chocolate cost analysis. Without it a single deeply nested query is a denial of service. This is GraphQL's equivalent of usage plans and is mandatory before any public exposure |
| **Persisted operation allowlist** | the API. Clients send a document hash, not a document. Removes arbitrary-query attacks and cuts request size |
| **Detailed health is not public** | `/health` reports per-module status and pending migration counts. Point the ALB at `/alive` and keep `/health` off the public listener or behind auth |
| Rate limiting | ASP.NET rate limiting in the API, or AWS WAF on the ALB |
| TLS, WAF | ALB + ACM |
| Authentication | JWT validated by the API on every request |

## Conventions

- **Top-level folders are lowercase**: `src/`, `docs/`, `requests/`. Directories inside `src/` keep
  Sentence case: `src/Modules/Catalog/`, `src/ServiceDefaults/`. Tooling directories keep their required
  names (`.husky`, `.config`, `artifacts`).
- **The API runs on local port 5100**, set in `src/Api/Properties/launchSettings.json`, so the checked-in
  `requests/api.http` stays valid across runs.

## Domain model

### Aggregate roots

| Context | Roots | Notable invariant |
|---|---|---|
| catalog | `Movie`, `Cinema`, `Auditorium`, `Showtime` | no double-booked auditorium; showtime within release window |
| seating | `SeatHold` | one active hold per seat per showtime |
| pricing | `PriceCard`, `Quote` | quote expires; totals immutable |
| ordering | `Order`, `BookingSaga` | references `HoldId`, never mutates seat state |
| payments | `Payment`, `SavedInstrument` | authorize reversible, capture terminal |
| ticketing | `Ticket` | redeemed exactly once |
| loyalty | `Membership` + append-only ledger | balance never negative |
| identity | `UserProfile` | — |
| concessions | `Menu` | — |

### Deliberate deviations from textbook DDD

**Seat-hold uniqueness is enforced by a database constraint, not an aggregate boundary.** The textbook model
makes `ShowtimeSeating` one aggregate owning all seats, with optimistic concurrency. That serializes every
concurrent booking for a popular showtime into a retry storm. `SeatHold` is its own root and the partial
unique index is the true guardian.

**Ticket redemption uniqueness is likewise a database constraint.** Same reasoning: single-row uniqueness
under contention is what a database does better than an aggregate.

**Loyalty balance is a stored projection on `Membership`, not a ledger replay.** Replaying thousands of
entries on every checkout is a slow read. The ledger entry and the balance update share one transaction, with
periodic reconciliation. Optimistic concurrency is correct here — one member's transactions *should*
serialize.

**`Auditorium` is its own root, not part of `Cinema`.** Otherwise loading a cinema loads every seat of every
screen.

**`Quote` is immutable with an expiry and is snapshotted into `Order` by value.** Nothing recomputes at
capture. This makes the corpus defect `B5` (service fee first revealed at the final total) structurally
impossible: the number shown is the number captured, or the quote expired and the user is told.

### Module ownership

A field belongs to the module that owns its data. A resolver needing another module's data goes through
that module's contract, never its `DbContext`.

| Type | Owner | Extended by |
|---|---|---|
| `Movie`, `Cinema`, `Showtime` | catalog | concessions, seating, pricing |
| `Order` | ordering | ticketing, payments |
| `User` | identity | ordering, loyalty |
| `Ticket` | ticketing | — |

**Seat map layout lives in `seating`**, denormalized from an `AuditoriumLayoutChanged` event.

**Every cross-module read is batched behind a DataLoader.** A 40-showtime list resolves prices in one
call into `pricing`, not forty. In-process makes N+1 cheap enough to hide in development and expensive
enough to hurt under load.

**Relay Global Object Identification is mandatory** (`AddGlobalObjectIdentification()`), with ids unique
across modules. The client's normalized store requires it and retrofitting is expensive.

## Client

- **.NET MAUI**, C# markup, no XAML. Shell navigation, MVVM, `CommunityToolkit.Mvvm`.
- **Strawberry Shake v15** for typed codegen against the composite schema, **with its reactive store used for
  offline**. The official `StrawberryShake.Persistence.SQLite` package is stalled at 11.3.4, so the
  store-persistence hook is hand-written against SQLite.
- **Tickets do not live in the reactive store.** They get a dedicated durable table written at issuance. A
  generic entity store may evict; a ticket at a turnstile may not.
- The persisted store holds PII and is encrypted (SQLCipher). Account deletion wipes it locally.
- **QR codes are self-verifying** — a signed token the door scanner validates offline, with redemption
  recorded online behind a local dedupe cache. A network blip at the door must not stop the queue.
- Posters are served **pre-sized** from S3 + CloudFront and consumed via `UriImageSource` with
  `CacheValidity`. No client-side image library; the resize happens once, server-side.
- One `DelegatingHandler` attaches the bearer token and refreshes on 401.
- `AppColors` is replaced by the token sheet in `CinepolisUI/design/01-tokens-color.png` before any screen work.
- `rules/maui-resilience.md` needs amending: it mandates repository-level caching, which now conflicts with
  the reactive store. Two caches with independent invalidation is a defect generator.

**Open risk:** Strawberry Shake codegen under full trimming on a Release iOS build is unproven and is spiked
in week one. Fallback is raw `HttpClient` with hand-written operations plus a CI contract test against the
gateway, which loses compile-time schema safety.

## Infrastructure

- **Laptop-first.** `make up` and `make dev` run the whole system locally. AWS is deployed on demand and destroyed after.
  Ephemeral environments enforce IaC discipline: drift and hand-clicked fixes surface the same week.
- **Persistent cloud floor ~$1/month** — Cognito (free at this scale), ECR, S3, SSM Parameter Store
  (free; Secrets Manager is $0.40/secret/month), CDK bootstrap, GitHub OIDC role.
- **Deployed stack** — 1 Fargate task, 1 RDS instance, ALB, VPC endpoints. The ALB is the floor; compute
  and storage round to noise at this size.
- **No NAT Gateway.** VPC endpoints cover ECR/Logs/Secrets; the PSP is simulated in-cluster and posters are
  pre-seeded to S3, so nothing needs egress. Saves ~$33/month. A `t4g.nano` NAT instance covers any future need.
- ALB is public and fronts the single task, which sits in a private subnet. No ECS Service Connect: there
  is nothing to connect.
- Two AZs (ALB requires it), one NAT-free path, no read replicas. Production would add HA; paying for HA
  that cannot be exercised is waste.
- The instance and the task stop together on one schedule, since a demo stack has no reason to idle.

### Local versus deployed

| Concern | Local | AWS |
|---|---|---|
| Postgres | 1 container, 1 database, schema per module | 1 RDS instance, schema per module |
| Schema | `make schema`, committed | same artifact, no runtime step |
| Events | in-process handlers, outbox table | same, plus SQS when a consumer needs independent retry |
| Auth | Cognito (real user pool) | Cognito |
| Telemetry | OTLP to any collector | ADOT → CloudWatch / X-Ray |
| Compute | `make up`, API container in Compose | 1 Fargate task behind an ALB |

Connection-string-per-environment keeps module code byte-identical across both.

## Delivery

### CI/CD

- GitHub Actions, one workflow: build → test → `dotnet publish /t:PublishContainer` → push to ECR tagged
  with the **git SHA** → deploy. No `latest`, no Dockerfile.
- **`make schema` runs on every PR and an uncommitted diff fails the build.** A schema change that the
  author did not notice cannot merge. This replaces composition as the breaking-change gate.
- **Migrations are a separate pipeline step**, run as a one-off task before the API deploys. Never on
  application startup, because replicas race and a failed migration should not take the app down with it.
- **Expand/contract for every schema change.** The deployed API and a released mobile client are always
  two versions apart, so the old shape has to keep working.

### Testing

| Layer | Tool |
|---|---|
| Domain units | xUnit + Shouldly |
| Per-service integration | **Testcontainers, real Postgres** |
| Architecture | NetArchTest |
| REST contracts | per boundary |
| Saga E2E | `WebApplicationFactory` + Testcontainers, happy path **and every compensation path** |
| Seat contention | k6 / NBomber |
| Device UI | Appium, **local only**, ~5 smoke flows, never a required check |

SQLite in-memory is replaced by Testcontainers. The concurrency design rests on a Postgres partial unique
index, `RETURNING`, and timezone handling — none of which exist in SQLite.

Appium flows: cold start → cartelera; showtime → seat select → hold; checkout with a declined card; offline
ticket render in airplane mode; account deletion. `AutomationId` is mandatory on every interactive control.

### Observability and SLOs

OpenTelemetry end to end — one trace from a MAUI tap through the API, the outbox and its consumer.
Any OTLP collector locally, ADOT → CloudWatch/X-Ray deployed. Nothing exports unless `OTEL_EXPORTER_OTLP_ENDPOINT` is set.

| SLO | Target |
|---|---|
| Seat map | p95 < 400ms |
| Hold mutation | p99 < 250ms |
| Checkout saga, end to end | p95 < 3s |
| Double-sold seats | zero, as a hard invariant with an alarm |

### Simulation harness

- **Seeded catalog** — ~12 cinemas across timezones, real auditorium layouts, ~40 films, showtimes generated
  14 days forward, posters pre-sized into S3.
- **PSP simulator** as a real service with real webhooks: configurable latency, declines, timeouts, duplicate
  delivery, out-of-order callbacks. A mock that always succeeds in 10ms tests nothing.
- **Synthetic traffic** — k6/NBomber booking flows with several virtual users contending for the same seats.
  This is what validates the partial unique index and exercises the seat-map subscription.

### Phases

| Phase | Delivers |
|---|---|
| 0 | Walking skeleton: Compose Postgres, `Cinema.Api` with ten modules, `ServiceDefaults`, module boundary test, `catalog` on real Postgres, OTel, design tokens, MAUI hello-world against the API, deployed once to AWS |
| 1 | Browse — catalog, cartelera, detail, horarios. Read-only, no auth |
| 2 | Identity — Cognito native SRP, auth screens, guest support |
| 3 | Booking spine — seating, pricing, ordering, payments, ticketing, the saga, seat map + subscription, PSP simulator, contention tests |
| 4 | Fulfilment — QR signing, offline store, Mis Compras, door redemption |
| 5 | Concessions |
| 6 | Loyalty, passes |
| 7 | Account — profile, favorites, settings, notifications, deletion saga |
| 8 | Ops — back-office, SLO alarms, full synthetic traffic |

Phase 0 ends with something deployed. Proving the pipeline before the domain avoids debugging CDK and the
saga simultaneously.

### Document order

ADRs (one MADR file per decision above) → domain model document → one PRD per phase, written on entering
that phase. Eight PRDs written upfront would be stale by phase 3.

## Verified versions

| Package | Version |
|---|---|
| .NET SDK | 10.0.302 |
| Hot Chocolate | 16.6.1 |
| HotChocolate.Types.Analyzers | 16.6.1 |
| OpenTelemetry | 1.18.0 |
| Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.3 |
| Strawberry Shake | 15.x |
| PostgreSQL (Compose image) | 18.3 |

Notes carrying real consequences:

- OpenTelemetry **1.14.0** carries known moderate advisories (unbounded OTLP response bodies; patched in
  1.15.2). Pinned to 1.18.0.
- Hot Chocolate's `[QueryType]` source generator emits a per-assembly `AddTypes()`; it must be called
  explicitly, as `builder.AddGraphQL().AddTypes()`. "Automatically registers" in the docs means the generator
  writes the method, not that it wires itself. Requires `HotChocolate.Types.Analyzers` referenced as an
  analyzer.
- PostgreSQL 18 images store data in a major-version subdirectory, so the volume mounts at
  `/var/lib/postgresql`, not `/var/lib/postgresql/data`. Mounting the old path makes the container start
  and then fail its health check. Volumes created on PG17 will not mount.
- The generated GraphQL registration method is named after each module's `[assembly: Module(...)]`
  attribute, so ten modules need ten distinct names. Ten `Module("Types")` attributes collide.
- Hot Chocolate 16 replaces `[UseProjection]` with `QueryContext<T>` and `.With()`. Combining the two
  raises analyzer **HC0099**, which `TreatWarningsAsErrors` turns into a build failure. `AddFiltering()`
  and `AddSorting()` must be registered for `QueryContext<T>` to work at all.
- Resolvers run in parallel and `DbContext` is not thread-safe, so a module registers
  `AddDbContextFactory<T>` and the schema registers `RegisterDbContextFactory<T>`. A scoped `DbContext`
  is wrong here, which in turn rules out `AddDbContextCheck<T>` and makes a hand-written health check the
  simpler path.
- Ten modules on one database share `__EFMigrationsHistory` and will fight over it unless each pins its
  own: `MigrationsHistoryTable("__EFMigrationsHistory", "<schema>")`.

## Open decisions

- **Cross-module transactions.** A mutation writing across two modules atomically has two `DbContext`
  instances over one database. Likely answer: a request-scoped `NpgsqlConnection` shared by every module
  context, with one transaction committed by a unit of work at the end of the mutation. Deferred until
  `seating` and `ordering` both write, because inventing a cross-module write to exercise it would settle
  the question against a fake case. If two modules constantly need one transaction, that is evidence they
  are one module, not evidence the pattern is wrong.

## Change log

| Date | Change |
|---|---|
| 2026-08-24 | Initial decision log from the architecture session. Scope, market, service topology, seat-hold model, federation, saga, identity, client, infrastructure, delivery, and phases recorded. |
| 2026-08-24 | Gateway composition deferred to phase 0.2: Fusion Aspire 16.6.1 routes local composition through Nitro-branded APIs, and there is no schema worth composing yet. Gateway project built but out of the run graph. |
| 2026-08-24 | Edge and gateway section added: AWS API Gateway, YARP and Ocelot rejected with reasons; ALB three-route topology; query cost analysis and persisted operations recorded as required hardening. Sentence-case folder convention and fixed local ports 5101-5110 recorded. |
| 2026-08-31 | Gateway composition is wired, no longer deferred: local `nitro fusion compose` → `Src/Gateway/gateway.far` → `AddFileSystemConfiguration`, gateway in the AppHost run graph. Nitro's hosted registry not adopted; CI composition covers breaking-change detection. |
| 2026-08-31 | Per-subgraph port pinning (5101-5110) dropped. Only the gateway is pinned, at 5100; every client query is federated through it, so `Requests/` holds one file rather than ten. |
| 2026-08-31 | Corrected the generated registration method: `AddTypes()`, not `Add<Service>Types()`. The documented form did not compile against Hot Chocolate 16.6.1. |
| 2026-08-31 | Folder convention reversed: top-level folders are lowercase (`src/`, `docs/`, `requests/`); directories inside `src/` keep Sentence case. Supersedes the Sentence-case rule recorded earlier the same day. |
| 2026-08-31 | Collapsed to a **modular monolith**. Supersedes decisions 1, 2, 3, 5 and 6. Ten services became ten module assemblies under `src/Modules` behind one `src/Api` host; the Fusion gateway, `gateway.far` and schema composition are deleted. Boundaries are enforced by `internal` plus a NetArchTest rule in `tests/Architecture`. Stated goal is full-stack breadth, and ten deployables spent that budget on coordination. |
| 2026-08-31 | Aspire removed. At one app and one database there is nothing to orchestrate; Compose runs Postgres. `ServiceDefaults` is kept minus service discovery. |
| 2026-08-31 | No Dockerfile. `dotnet publish /t:PublishContainer` builds the image from the SDK, verified against `mcr.microsoft.com/dotnet/aspnet:10.0`. |
| 2026-08-31 | Deployment shape reduced to 1 Fargate task, 1 RDS instance, 1 ALB, superseding 11 tasks and 10 Aurora clusters. App Runner evaluated and unavailable: closed to new customers. |
| 2026-08-31 | Decision 4 restated: REST is for callers we do not control, not for service-to-service. Nothing internal is HTTP now that modules call each other in-process. Per-module GraphQL and REST surface recorded; `notifications` has none. |
| 2026-08-31 | Module communication order recorded: snapshot by event first, integration event second, synchronous contract call last. Only the third changes shape on extraction. |
| 2026-08-31 | Hot Chocolate 16 data-access constraints recorded: `QueryContext<T>` replaces `[UseProjection]` and mixing them fails the build via HC0099; `DbContextFactory` over scoped `DbContext`; per-module migrations history table. |
| 2026-08-31 | Infrastructure and delivery restated for one deployable: ALB fronts a single Fargate task, no ECS Service Connect, one RDS instance, one CI workflow publishing via the SDK container target. `make schema` diff replaces composition as the PR gate. |
| 2026-08-31 | Catalog built as the reference module: `Movie`, `catalog` schema, migration, and a `QueryContext<T>` read path verified emitting column-projected, filter-pushed SQL. The other nine modules are empty assemblies; a module gets a `DbContext` with its first entity. `/health` now maps in every environment and reports per-module migration status. Unused package declarations removed, including the two SQLite packages this log rules out. |
| 2026-08-31 | The API runs in Compose alongside Postgres, so local matches deployed in shape. The image still comes from `dotnet publish /t:PublishContainer` with no Dockerfile; Compose consumes the tag. `make dev` remains for host-process iteration. |
