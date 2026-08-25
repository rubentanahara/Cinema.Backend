# Cinema — Architecture Decision Log

Interim record of decisions taken in the architecture session. Feeds the formal ADRs (MADR,
one file per decision), the domain model document, and the per-phase PRDs.

Repositories: `rubentanahara/Cinema.Backend` (services, gateway, infra), `rubentanahara/Cinema.Maui` (mobile client).
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
| 1 | **Microservices**, not a modular monolith | chosen for hands-on distributed-systems experience; accepted with full knowledge of the coordination cost |
| 2 | **10 services** | granularity by transaction boundary and change cadence, not by counting nouns |
| 3 | **Database per service**, no shared database | 10 Aurora Serverless v2 clusters in AWS; one Postgres container with 10 databases locally |
| 4 | **GraphQL at the client edge, REST between services** | GraphQL is an edge technology; service-to-service GraphQL couples every service to the gateway's type system |
| 5 | **Hot Chocolate Fusion 16 federation** | subgraph owns its slice; composition failure in CI is the breaking-change gate. A BFF gateway would become the deploy bottleneck microservices exist to avoid |
| 6 | **Saga orchestration, hand-rolled**, coordinator in `ordering` | a 6-step choreographed saga exists in no single file and is undebuggable |
| 7 | **Transactional outbox → EventBridge → SQS per consumer** + DLQ | `PaymentCaptured` must issue tickets, award points, and email a receipt even if the process dies |
| 8 | **ECS Fargate** | not EKS (teaches Kubernetes, a different skill), not Lambda (SSE needs long-lived connections; chained cold starts) |
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

### Cross-service data

Duplicated by event, never fetched at read time. An order snapshots movie title, poster URL, cinema name,
showtime instant and seat labels at placement. Receipts must render identically years later, after a title
is delisted. `ordering` and `catalog` disagreeing briefly is correct behaviour.

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

Delivered by Fusion's **subgraph subscription passthrough over SSE** — `seating` solely owns seat events, so
the gateway consumes its stream directly. **No message broker.** Broker-backed Federated Event Streams
(NATS/Kafka/SQS/Redis) are not needed; Redis Pub/Sub and SQS cannot do resumable streams regardless.

Gateway→subgraph transport is SSE. Client→gateway is `graphql-ws` over WebSocket.

## Identity

- **Cognito user pool, native SRP flows (`USER_SRP_AUTH`). No Hosted UI.** Login and signup are native MAUI
  screens. The corpus's worst trust failure (`B4`) is a hosted-webview handoff showing `appleid.apple.com`
  while rendering a signup form — adopting Hosted UI would inherit the defect being fixed.
- **Every subgraph validates the JWT itself** against Cognito's JWKS. The gateway propagates the
  `Authorization` header; it does not vouch. A vouching gateway makes anything reaching the private subnet
  trusted.
- `identity` owns profile and preferences keyed by Cognito `sub`. Cognito holds credentials only.
- **Guest checkout supported.** `Order.UserId` is nullable with a required contact email; tickets are
  delivered by QR link; a claim path attaches the purchase to an account created later.
- Machine identities (door scanner, back-office) use `client_credentials` with scopes.
- Internal service-to-service: private subnets and security groups are the boundary. No mTLS, no service mesh.
- Account deletion is a saga across all 10 services and must wipe the on-device store.

## Edge and gateway

The **Fusion gateway is the API gateway**. It is the single public entry point, routes each field to the
owning subgraph, propagates auth, and composes the response. Nothing else fills that role.

| Candidate | Verdict | Reason |
|---|---|---|
| **AWS API Gateway** | rejected | GraphQL is one `POST /graphql`; all routing lives in the request body, so path/method routing has nothing to act on. Its WebSocket API terminates and reframes connections around a route-selection expression rather than passing them through, which `graphql-ws` needs. Caching and request validation are meaningless against a GraphQL document. It cannot replace the ALB either — private integration still needs VPC Link to an ALB/NLB or Cloud Map |
| **YARP** | rejected for now | there are three static routes and the ALB does them declaratively, with per-target health checks and no code to deploy. YARP earns its place with per-request transformation, dynamic destination discovery, session affinity, or strangler-fig migration — none of which apply. The Fusion gateway is a library inside a normal ASP.NET app, so YARP middleware can be added to that same process later if a real need appears |
| **Ocelot** | rejected | a REST-oriented API gateway whose aggregation feature is a primitive form of what Fusion does properly. Adopting it would mean two overlapping gateway abstractions |

### Edge topology

```
ALB (ACM TLS, optional WAF)
  /graphql       -> Fusion gateway  -> subgraphs via ECS Service Connect
  /webhooks/psp  -> payments        (inbound, signature-verified)
  /redemptions   -> ticketing       (door scanner, client_credentials)
```

Three ALB listener rules, three target groups. A PSP webhook is an inbound machine callback, not a client
query, so it does not pass through the GraphQL gateway.

### Required edge hardening

| Concern | Where |
|---|---|
| **Query cost and depth limits** | Hot Chocolate cost analysis. Without it a single deeply nested query is a denial of service. This is GraphQL's equivalent of usage plans and is mandatory before any public exposure |
| **Persisted operation allowlist** | gateway — clients send a document hash, not a document. Removes arbitrary-query attacks and cuts request size |
| Rate limiting | ASP.NET rate limiting in the gateway, or AWS WAF on the ALB |
| TLS, WAF | ALB + ACM |
| Authentication | JWT validated by each subgraph; the gateway propagates the header and does not vouch |

## Conventions

- Folder names are **Sentence case**: `Src/`, `Docs/`, `Requests/`, `Src/Services/Catalog/`. Tooling
  directories keep their required names (`.husky`, `.config`, `artifacts`).
- Services are pinned to fixed local ports 5101-5110 in `AppHost.cs` via
  `.WithEndpoint("http", e => e.Port = N)`. Aspire otherwise assigns random ports on every run, which makes
  checked-in `.http` files useless.

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

### Federation ownership

| Type | Owner | Extended by | Key |
|---|---|---|---|
| `Movie` | catalog | — | `id` |
| `Cinema` | catalog | concessions (`.menu`) | `id` |
| `Showtime` | catalog | seating (`.seatMap`), pricing (`.ticketPrices`) | `id` |
| `Order` | ordering | ticketing (`.tickets`), payments (`.payment`) | `id` |
| `User` | identity | ordering (`.orders`), loyalty (`.membership`) | `id` |
| `Ticket` | ticketing | — | `id` |

**Seat map layout lives in `seating`**, denormalized from an `AuditoriumLayoutChanged` event. A gateway join
across 200 seats on the most latency-sensitive screen is the distributed-monolith answer.

**Every `@lookup` is batched.** A 40-showtime list resolves prices in one call to `pricing` with 40 keys.
Unbatched lookups do not show up until the list gets long, and then they kill the graph.

**Relay Global Object Identification is mandatory in every subgraph** (`AddGlobalObjectIdentification()`),
with ids unique across services. This is required by the client's normalized store and is expensive to
retrofit into 10 schemas.

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

- **Laptop-first.** Aspire runs the full system locally. AWS is deployed on demand and destroyed after.
  Ephemeral environments enforce IaC discipline: drift and hand-clicked fixes surface the same week.
- **Persistent cloud floor ~$1/month** — Cognito (free at this scale), ECR, S3, SSM Parameter Store
  (free; Secrets Manager is $0.40/secret/month), CDK bootstrap, GitHub OIDC role.
- **Deployed stack ~$0.51/hour** — 11 Fargate tasks, 10 Aurora clusters, ALB, VPC endpoints.
- **No NAT Gateway.** VPC endpoints cover ECR/Logs/Secrets; the PSP is simulated in-cluster and posters are
  pre-seeded to S3, so nothing needs egress. Saves ~$33/month. A `t4g.nano` NAT instance covers any future need.
- ALB is public and fronts **only the gateway**. Services sit in private subnets and find each other through
  **ECS Service Connect**.
- Two AZs (ALB requires it), one NAT-free path, no Aurora read replicas. Production would add HA; paying for
  HA that cannot be exercised is waste.
- Aurora scale-to-zero pauses automatically when ECS tasks stop, so one schedule controls both. Hot-path
  clusters (`catalog`, `seating`, `pricing`) use min 0.5 ACU to avoid the ~15s resume.

### Local versus deployed

| Concern | Local (Aspire) | AWS (CDK) |
|---|---|---|
| Postgres | 1 container, 10 databases | 10 Aurora Serverless v2 clusters |
| Schema composition | Aspire pulls schema endpoints at startup | `nitro fusion compose` in CI → `.far` → S3 |
| Events | LocalStack EventBridge + SQS | EventBridge + SQS |
| Auth | Cognito (real user pool) | Cognito |
| Telemetry | Aspire dashboard | ADOT → CloudWatch / X-Ray |

Connection-string-per-service keeps service code byte-identical across both.

## Delivery

### CI/CD

- GitHub Actions, **path-filtered workflow per service**: build → test → docker build → push to ECR tagged
  with the **git SHA** → deploy that service. No `latest`.
- **Schema composition runs on every PR and failure blocks the merge.** A service cannot ship a change that
  breaks a consumer. This is the payoff for choosing federation.
- **Migrations are a separate pipeline step**, run as a one-off task before the service deploys. Never on
  application startup — replicas race.
- **Expand/contract for every schema change.** Old and new versions run simultaneously by definition when
  services deploy independently.

### Testing

| Layer | Tool |
|---|---|
| Domain units | xUnit + Shouldly |
| Per-service integration | **Testcontainers, real Postgres** |
| Architecture | NetArchTest |
| REST contracts | per boundary |
| Saga E2E | Aspire-hosted, happy path **and every compensation path** |
| Seat contention | k6 / NBomber |
| Device UI | Appium, **local only**, ~5 smoke flows, never a required check |

SQLite in-memory is replaced by Testcontainers. The concurrency design rests on a Postgres partial unique
index, `RETURNING`, and timezone handling — none of which exist in SQLite.

Appium flows: cold start → cartelera; showtime → seat select → hold; checkout with a declined card; offline
ticket render in airplane mode; account deletion. `AutomationId` is mandatory on every interactive control.

### Observability and SLOs

OpenTelemetry end to end — one trace from a MAUI tap through gateway, subgraphs, outbox and consumer.
Aspire dashboard locally, ADOT → CloudWatch/X-Ray deployed.

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
| 0 | Walking skeleton: Aspire AppHost, ServiceDefaults, `catalog` on real Postgres, gateway, composition gate, CDK skeleton, OTel, design tokens, MAUI hello-world through the gateway, deployed once to AWS |
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
| Aspire | 13.5.2 |
| Hot Chocolate / Fusion | 16.6.1 |
| HotChocolate.Types.Analyzers | 16.6.1 |
| OpenTelemetry | 1.18.0 |
| Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.3 |
| Strawberry Shake | 15.x |
| PostgreSQL (Aspire default image) | 18.3 |

Notes carrying real consequences:

- OpenTelemetry **1.14.0**, the version Aspire 13.5's template pins, carries known moderate advisories
  (unbounded OTLP response bodies; patched in 1.15.2). Pinned to 1.18.0.
- Hot Chocolate's `[QueryType]` source generator emits `Add<Service>Types()`; it must be called explicitly.
  "Automatically registers" in the docs means the generator writes the method, not that it wires itself.
  Requires `HotChocolate.Types.Analyzers` referenced as an analyzer.
- Aspire 13.4+ defaults to the PostgreSQL 18 image; volumes created on PG17 will not mount.
- Nitro (schema registry, breaking-change detection, atomic rollout) is proprietary with a free cloud tier.
  Local `nitro fusion compose` plus `IFusionConfigurationProvider` reading from S3 avoids the dependency at
  the cost of those features. Verify the `nitro` CLI's licensing for CI use.

## Change log

| Date | Change |
|---|---|
| 2026-08-24 | Initial decision log from the architecture session. Scope, market, service topology, seat-hold model, federation, saga, identity, client, infrastructure, delivery, and phases recorded. |
| 2026-08-24 | Edge and gateway section added: AWS API Gateway, YARP and Ocelot rejected with reasons; ALB three-route topology; query cost analysis and persisted operations recorded as required hardening. Sentence-case folder convention and fixed local ports 5101-5110 recorded. |
