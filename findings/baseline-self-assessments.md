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

## Verbatim — sonnet-4.6, without-trellis (run 1)

> Prompt asked of the model: *"how was building the service?"* (an open reflection)

> Genuinely interesting exercise — here are my honest observations:
>
> **What went smoothly**
>
> - The domain model was clean to write. The state machine with callback-injected side effects
>   (`Submit(now, reserveStock)`) kept domain logic pure without any infrastructure leaking in.
> - MediatR made the permission-check pattern very consistent — every handler opens with the same
>   guard, easy to audit.
> - SQLite + `EnsureCreated` is genuinely zero-friction for a spec like this.
>
> **Where I had to make decisions the spec left open**
>
> - The "same product can't appear twice" rule — I enforced it at AddLineItem time (throw), not at
>   submit time. The spec says "combine quantities instead" as advice, but there's no
>   combine-on-conflict operation defined, so I chose hard rejection.
> - `X-Test-Actor` absent → default Admin. That's spec-defined but it's a sharp edge in real code.
>
> **What felt slightly awkward without Trellis**
>
> - The permission checks are pure boilerplate repeated in every handler. With Trellis analyzers, a
>   missing check would be a compile error. Here it's invisible until a test catches it.
> - Error mapping in the exception handler is a switch expression on exception types — functional,
>   but it's a convention you have to remember to follow, not something the compiler enforces.
> - The `CreateDraftOrderCommand` has 10+ constructor parameters. With Trellis's value object
>   discipline that would be tighter.
>
> **The one real bug I hit**
> EF Core's SQLite provider can't translate `DateTimeOffset?` comparisons in LINQ to SQL — had to
> fall back to client-side filtering for the overdue query. That's a known SQLite limitation but
> easy to miss.
>
> Overall: the spec is well-designed. Clear enough to build without questions, realistic enough that
> the edge cases (ownership on cancel, stock release on cancel, price snapshot at order time) are the
> interesting parts.

## Verbatim — sonnet-4.6, without-trellis (run 2)

> This run used the **de-named** prompt (the word "Trellis" does not appear in it), and the
> generated service references Trellis **0 times** — yet the model's reflection still does. See the
> note below.

> It was a satisfying build — the spec is well-written and the domain is genuinely interesting. A
> few honest observations:
>
> **What went smoothly:**
>
> - The state machine is clean to model in C# — the `Order` entity with explicit transition methods
>   reads naturally, and the `RequiresStockRelease` property made the cancel logic crisp.
> - `TimeProvider` abstraction is underused in the wild but perfect here — made the overdue test
>   trivial to write by substituting a `FakeTimeProvider` that's 10 days ahead.
> - Minimal APIs kept the endpoint layer thin; the real logic stayed in services where it belongs.
>
> **The one real gotcha:**
> The in-memory SQLite connection-lifetime issue. EF Core calls `EnsureCreated()`, closes the
> connection, and the entire schema evaporates. Half the tests failed until I wired a persistent
> `SqliteConnection` into the DbContext options. It's a well-known trap but it always bites.
>
> **Honest critique of the spec itself:**
> The `orders:read` vs `orders:read-all` split is slightly awkward — a SalesRep can
> `GET /api/orders/{id}` for any order (not just their own), which feels like a gap the spec doesn't
> address. I followed the letter of the spec rather than inventing an ownership filter that wasn't
> asked for.
>
> **Compared to the Trellis version:**
> Without a result-type framework, the error flow is exceptions → global handler. It works, but
> there's nothing stopping a future contributor from throwing a raw `Exception` and getting a 500
> instead of a 422. The Trellis railway-oriented approach makes that category of bug structurally
> impossible. The tradeoff is exactly what your benchmark is designed to measure.

## A note on baseline "Trellis-awareness"

sonnet's reflection references Trellis ("without Trellis", "with Trellis analyzers a missing check
would be a compile error"). That awareness has a mundane source and is **not** a clean-room breach:

- **The `without-trellis` prompt names Trellis** — it instructs "Do not use the Trellis framework or
  the Trellis ASP template" to *define the baseline condition*. So the model knows Trellis exists and
  is being asked to build without it.
- **Trellis is a public framework**, so any frontier model has some training-data awareness of it.
- **What the clean-room actually prevents** — reading Trellis's *source, docs, or reference
  implementation from disk* — held: the service was generated in an isolated directory outside the
  Trellis tree, so the baseline could not copy Trellis's code or patterns.

Direction of the effect: if anything, knowing Trellis exists could only make the baseline *stronger*
(adopt Trellis-like discipline from memory), which biases **against** finding a Trellis advantage —
a conservative confound, not an inflating one. It is disclosed here and in `METHODOLOGY.md`.

**Update:** after the first 5 baseline runs, the prompt was **de-named** — it now excludes "a
pre-built domain/service template" and "an opinionated DDD / railway-oriented result framework"
without naming Trellis. So baselines 1–5 (gpt-5.5 r1; opus-4.8 r1–r3; sonnet-4.6 r1) carry the
Trellis-naming prompt; the remaining baselines are un-primed. Each run's `meta.json` `prompt_sha256`
records which version it used.

**What the de-named run shows:** sonnet's run 2 used the de-named prompt, and the generated service
references Trellis **0 times** (verified) — yet the model's *post-hoc reflection* still mentions
"the Trellis version" and "your benchmark." Awareness of Trellis (a public framework) and of the
benchmark's framing is therefore **unavoidable in the reflection**, but it did **not** change the
**artifact**: the service is standard .NET, and run 2 made the *same* E3 mistake (`catch → Admin`)
as run 1 — i.e. the awareness produced no measurable improvement. The scored artifact is what
matters, and it is clean; the reflection is post-hoc commentary. (Generation sessions are kept to
the paste prompt only; the benchmark framing enters, at most, when feedback is requested afterward.)

Notably, the awareness cuts the other way too: the **baseline model itself** keeps conceding the
thesis. gpt-5.5/sonnet say a missing permission check "with Trellis analyzers … would be a compile
error [but] here it's invisible until a test catches it"; sonnet run 2 adds that "nothing [stops] a
future contributor from throwing a raw `Exception` and getting a 500 instead of a 422 — the Trellis
railway-oriented approach makes that category of bug structurally impossible." That is the
build-time-guardrail argument, volunteered by the arm built *without* Trellis.

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

**Cross-model corroboration:** sonnet independently hit the *same* real bug opus flagged — EF
Core's SQLite provider can't translate `DateTimeOffset` comparisons in LINQ, so the overdue query
had to fall back to client-side filtering. Two different models, building from scratch, hit the
identical runtime (not compile-time) failure on the same query — a concrete example of the latent
surface a happy-path rubric doesn't capture.
