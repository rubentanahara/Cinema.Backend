# Service boundaries

Ten services, each owning one database. `docs/architecture-decisions.md` carries the reasoning; this file
carries the rules that follow from it.

- **A service never reads another service's database.** No shared connection string, no cross-schema query,
  no "just this once" read model over someone else's tables.
- Cross-service reads go through a contract interface. Cross-service data is **duplicated by event and stored
  as a snapshot**, never fetched at read time.
- An order snapshots movie title, poster URL, cinema name, showtime instant and seat labels at placement. A
  receipt must render identically years after the title is delisted. `ordering` and `catalog` disagreeing
  briefly is correct behaviour, not a bug to fix.
- **Back-office is not a service.** Admin operations are role-gated mutations on the service owning the data.
- New capability? Find the service that owns the data first. A new service needs a transaction boundary and a
  change cadence of its own, not a new noun.

## Events

- Publish through the **transactional outbox** — the domain write and the outbox row share one transaction.
  Never publish to a broker inside a request path and hope both landed.
- Every consumer is idempotent. EventBridge and SQS deliver at least once; duplicate delivery is normal
  traffic, not an incident.
- Every consumer has a DLQ. A poisoned message parks, it does not spin.

## Sagas

- The coordinator lives in `ordering` and is **hand-rolled and explicit**. A step is a method you can read.
- Every step has a compensation, and every compensation path is tested, not just the happy path.
- Compensations are idempotent and safe to run twice.

## Aggregates

- Single-row uniqueness under contention is a **database constraint**, not an aggregate boundary. Seat holds
  and ticket redemption are both enforced by a partial unique index. Do not replace one with optimistic
  concurrency on a fat aggregate.
- A hold is a **row, not a lock**. No transaction stays open across human think-time.
- Expiry is a **predicate** (`expires_at > now()`), never a scheduled job. Correctness never depends on a
  cron firing.
- Money and totals are immutable once quoted: `Quote` is snapshotted into `Order` by value and nothing
  recomputes at capture.
- `Auditorium` is its own root, not part of `Cinema`.
