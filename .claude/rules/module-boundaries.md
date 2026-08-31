# Module boundaries

Ten modules under `src/Modules`, one assembly each, one process, one database.
`docs/architecture-decisions.md` carries the reasoning; this file carries the rules that follow.

- **A module is its own assembly.** `internal` is therefore a real boundary, checked by the compiler.
  Keep types `internal` by default and make public only what another module legitimately needs.
- **No module project references another module project.** `tests/Architecture` fails if one does. When
  a genuine cross-module call appears, both sides reference a shared contracts assembly; the consumer
  never references the producer.
- **A module owns its schema in the database.** `catalog."Movies"`, `ordering."Orders"`. One `DbContext`
  per module with `HasDefaultSchema`, plus its own `MigrationsHistoryTable("__EFMigrationsHistory",
  "<schema>")` or modules fight over one history table. No foreign key crosses a schema line, and no
  query joins across one.
- Cross-module data is **duplicated by event and stored as a snapshot**, never read across a boundary.
- An order snapshots movie title, poster URL, cinema name, showtime instant and seat labels at
  placement. A receipt must render identically years after the title is delisted. This is a domain rule
  about receipts, not a workaround for distribution, so it survives being in one process.
- **Back-office is not a module.** Admin operations are role-gated mutations on the module owning the
  data.
- New capability? Find the module that owns the data first. A new module needs a transaction boundary
  and a change cadence of its own, not a new noun.

## Modules are capabilities, not layers

`Catalog/`, `Seating/`, `Ordering/`. Never `Controllers/`, `Services/`, `Repositories/` at the top
level. Layering lives inside a module, not above it.

## Events

- Publish through the **transactional outbox** — the domain write and the outbox row share one
  transaction. One outbox table, not ten.
- Every consumer is idempotent and has a dead-letter path. A poisoned message parks, it does not spin.
- In-process handlers are the default. Reach for a broker when a consumer genuinely needs to fail and
  retry independently of the request that produced the event.

## Sagas

Most of the booking flow is now **one database transaction**: commit the hold, write the order, issue
the ticket. Use it. A saga that coordinates two tables in the same database is ceremony.

The saga that remains is the one around **payments**, because the PSP is genuinely remote: it times
out, it double-delivers callbacks, it fails after taking the money. That step keeps explicit
compensation, and every compensation path is tested, not just the happy path.

## Aggregates

- Single-row uniqueness under contention is a **database constraint**, not an aggregate boundary. Seat
  holds and ticket redemption are both enforced by a partial unique index. This was never a
  microservices technique and does not change here.
- A hold is a **row, not a lock**. No transaction stays open across human think-time.
- Expiry is a **predicate** (`expires_at > now()`), never a scheduled job. Correctness never depends on
  a cron firing.
- `Quote` is immutable, snapshotted into `Order` by value. Nothing recomputes at capture.
- `Auditorium` is its own root, not part of `Cinema`.
