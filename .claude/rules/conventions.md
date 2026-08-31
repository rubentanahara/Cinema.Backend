# Conventions

## C#

- Every async method takes a `CancellationToken` and passes it down. No `async void` outside event handlers.
- Return an empty collection, never null. Do not pass null either.
- Exceptions over sentinel returns, with enough context in the message to act on. No empty catch.
- A new module copies `catalog`. The full recipe is in `CLAUDE.md` under "Adding a module"; the shape is
  `Domain/`, `Infrastructure/`, `Graph/`, a `<Name>Module.cs`, and a `Properties/ModuleInfo.cs` declaring
  a distinct `Module("<Name>Types")`.

## Service defaults

`Cinema.ServiceDefaults` supplies OpenTelemetry, health checks and the standard HTTP resilience handler.
`src/Api/Program.cs` calls `builder.AddServiceDefaults()` and `app.MapDefaultEndpoints()` — do not hand-roll
any of the three. Service discovery was dropped with Aspire; one process has nothing to discover.

`MapDefaultEndpoints` maps `/health` and `/alive` in **every** environment. It used to map them only in
Development, which would have made the ALB health check 404 and cycle the Fargate task.

Give every module-scoped `DbContext` a check with `AddModuleCheck<TContext>(Schema)`. It reports pending
migrations, so `/health` degrades for a reason that is true rather than echoing a hardcoded string.

## Testing

| Layer | Tool |
|---|---|
| Domain units | xUnit + Shouldly |
| Per-module integration | Testcontainers, real Postgres, through `WebApplicationFactory<Program>` |
| Architecture | NetArchTest |
| Saga E2E | `WebApplicationFactory` + Testcontainers, happy path and every compensation path |
| Seat contention | k6 / NBomber |

Not SQLite in-memory. The concurrency design rests on a Postgres partial unique index, `RETURNING`, and
timezone handling; none of the three exist in SQLite, so a green SQLite suite proves nothing about the
invariant that matters.

## Observability

- One trace from client tap through the API, the outbox and its consumer. Instrument across the outbox
  boundary; a trace that stops at the publish is half a trace.
- `ILogger<T>` from DI. Never log tokens, emails, card data, or seat-holder names.
- Budgets: seat map p95 < 400ms, hold mutation p99 < 250ms, checkout saga p95 < 3s, double-sold seats zero.
