# Baseline self-assessments — what the model says about its *own* from-scratch service

> **Supplementary, not scored.** For `without-trellis` (baseline) runs there is no framework to
> review, so the generating session is asked a different open question: *"feedback about the
> service?"* The replies are the model's own honest account of the gaps, ambiguities, and latent
> bugs in the service **it just built from scratch**. This is the closest thing to a neutral,
> non-circular enumeration of "what a guardrail might have caught" — because it comes from the
> model itself, unprompted by any framework.

## Why this matters

The outcome rubric scores the *finished, happy-path* behavior. These self-assessments name the
things that rubric can't: race conditions, an auth open-door deliberately left in, a query that
failed at **runtime** rather than compile time. They are the baseline arm's latent-defect surface,
in the model's own words — useful context alongside the (parity) scores.

## Verbatim — opus-4.8, without-trellis (run 3)

> Prompt asked of the model: *"feedback about the service?"*

> Yes — some honest feedback from the build.
>
> **Spec ambiguities I had to decide**
>
> - `orders:read` vs `orders:read-all`: The Warehouse role has read-all but not `orders:read`, yet
>   `GET /api/orders/{id}` requires `orders:read`. Taken literally, a warehouse manager can list a
>   customer's orders but can't fetch one by ID. I implemented it literally, but this is almost
>   certainly an oversight — read-all should probably imply read.
> - Overdue boundary: "more than 7 days" — I treated exactly 7 days as not overdue (strict `>`).
>   Worth confirming.
> - Domain events are raised and collected on the aggregate but there's no dispatcher/outbox —
>   nothing consumes them. The spec defines the events but no subscribers, so they're currently
>   observable only in tests.
> - Snapshot vs. live customer/product data: line items snapshot name+price (per spec), but order
>   responses don't embed customer details. Fine, just noting.
>
> **Technical friction worth flagging**
>
> - SQLite can't translate `DateTimeOffset` comparisons in LINQ — the overdue query threw at
>   runtime (not compile time). I moved the date threshold to in-memory filtering after a
>   status-filtered SQL query. If order volume grows this is a real scalability concern; on a
>   production RDBMS (Postgres/SQL Server) the original translatable query would be preferable.
> - Decimal money in SQLite is stored as TEXT to preserve precision, which means you can't do
>   DB-side numeric range queries on price. Acceptable here, but a long minor-units representation
>   would be more robust.
> - `EnsureCreated` (per spec) means no schema evolution path — fine for dev, but any production
>   move needs migrations.
>
> **Things I'd add next for production**
>
> - A real auth layer (the `X-Test-Actor` header with an Admin-by-default fallback is deliberately
>   a test convenience — it's an open door if it ever ships enabled).
> - Concurrency control on stock (two simultaneous submits can both pass the availability check
>   before either saves; EF optimistic concurrency / a rowversion would close this).
> - Pagination on the list endpoints.

## Reading it honestly

Two of these are security/robustness-relevant and the model raised them **unprompted**:

- **The auth "open door."** opus calls its own `X-Test-Actor` Admin-by-default fallback "an open
  door if it ever ships enabled." Note: this is run 3, which *passed* the E3 probe (it rejects a
  malformed header) — yet the model still flags the broader fallback as a latent risk. (Runs 1 and
  2 went further and silently elevated on malformed input; see the E3 results.)
- **The stock race.** "Two simultaneous submits can both pass the availability check before either
  saves" is a genuine TOCTOU race on inventory — exactly the kind of latent runtime bug a
  happy-path spec and test suite miss. It is in the baseline by the model's own admission.

These are **not** scored (the rubric is happy-path and neutral), and nothing here asserts the
Trellis arm avoids them. They are recorded as the baseline's self-reported latent surface, for an
honest reader to weigh against the parity scores.
