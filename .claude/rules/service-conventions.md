# Service conventions

## C#

- Every async method takes a `CancellationToken` and passes it down. No `async void` outside event handlers.
- Return an empty collection, never null. Do not pass null either.
- Exceptions over sentinel returns, with enough context in the message to act on. No empty catch.
- A new service copies the shape of an existing one: `Program.cs`, `Types/`, `Properties/ModuleInfo.cs`, and
  `AddServiceDefaults()` before anything else.

## Service defaults

`Cinema.ServiceDefaults` supplies OpenTelemetry, health checks and the standard HTTP resilience handler.
Call `builder.AddServiceDefaults()` and `app.MapDefaultEndpoints()` in every entry point — do not hand-roll
any of the three. Service discovery was dropped with Aspire; one process has nothing to discover.

`MapDefaultEndpoints` maps `/health` and `/alive` **only in Development**. A health probe returning 404 in
another environment is that, not an outage.

## Testing

| Layer | Tool |
|---|---|
| Domain units | xUnit + Shouldly |
| Per-service integration | Testcontainers, real Postgres |
| Architecture | NetArchTest |
| Saga E2E | Aspire-hosted, happy path and every compensation path |
| Seat contention | k6 / NBomber |

Not SQLite in-memory. The concurrency design rests on a Postgres partial unique index, `RETURNING`, and
timezone handling; none of the three exist in SQLite, so a green SQLite suite proves nothing about the
invariant that matters.

## Observability

- One trace from client tap through gateway, subgraph, outbox and consumer. Instrument across the outbox
  boundary; a trace that stops at the publish is half a trace.
- `ILogger<T>` from DI. Never log tokens, emails, card data, or seat-holder names.
- Budgets: seat map p95 < 400ms, hold mutation p99 < 250ms, checkout saga p95 < 3s, double-sold seats zero.
