# Qualitative finding — what the generating model says Trellis did

> **This is supplementary, and explicitly NOT part of the scoring.** The
> [rubric](../rubric/neutral-rubric.md) remains outcome-only and framework-agnostic. This file
> records the **generating model's own first-person assessment** of building on Trellis,
> collected immediately after it produced a `with-trellis` service. It is included because it
> names the *mechanism* behind the numbers — something an outcome rubric on a fully-specified
> spec cannot measure.

## Why this matters to the result

The quantitative result is, so far, **parity**: for this well-specified spec, a frontier model
reaches a fully spec-compliant, secure-in-Production service **with or without** Trellis (30/30
either way). Taken alone, that understates Trellis — because a rubric scored on the *final*
working state cannot see the wrong turns that never reached the final state.

The generating model fills that gap. It reports that Trellis converted a class of **likely
codegen mistakes into build-time errors** — caught early and loudly — instead of letting them
become subtle runtime bugs. On a complete spec with a thorough test suite, many of those latent
bugs would be caught anyway; on a real, under-specified, evolving codebase, they would not. That
is the crux of the value, and it is upstream of any outcome the rubric records.

## Verbatim feedback — GPT-5.5

Collected after each `with-trellis` generation; the same open question was asked of the
session that built the service.

### Run 1

> Prompt asked of the model: *"what feedback do you have about the trellis framework?"*

> Strong framework overall. The biggest win is that Trellis turned many likely AI/codegen
> mistakes into build-time feedback: unsafe `Maybe.Value`, unhandled `Result`, wrong event
> interfaces, EF `Maybe` query shape, and invalid handler flows were all caught early and
> clearly.
>
> My main feedback:
>
> 1. **The guardrails are genuinely useful.** The analyzers and API conventions made the service
>    converge toward correct ROP, explicit errors, safe optional handling, and EF-safe queries.
> 2. **The reference docs are essential but dense.** They prevented wrong guesses, but building a
>    full service requires jumping across many docs. A "greenfield CRUD + state machine + EF +
>    auth checklist" would reduce friction.
> 3. **Error HTTP defaults may surprise specs.** `InvalidInput → 422` is reasonable, but many
>    specs expect `400`. The override was easy once found, but worth making prominent.
> 4. **EF collection mapping needs clearer examples.** Mapping a private backing collection plus
>    exposing read-only `IReadOnlyCollection<T>` needed explicit `Ignore`. A canonical
>    aggregate-with-owned-collection sample would help.
> 5. **Testing ergonomics are good, but discoverability could improve.**
>    `AggregateTestMutator.SetMaybeField` was useful, but easy to misremember. The testing docs
>    helped, but examples for time-shifting aggregate state would be handy.
> 6. **The framework is AI-friendly.** The biggest value is not reducing code volume; it's
>    narrowing the space of acceptable code so generated implementations fail loudly instead of
>    becoming subtle runtime bugs.

### Run 2

> Trellis gave strong guardrails, but the template/API surface still has some friction.
>
> **What worked well**
>
> - Source generators and analyzers caught real issues early, especially discarded `Result`
>   values and EF `Maybe<T>` conventions.
> - `Required*` value objects made validation explicit and pushed bad input handling to the
>   boundary.
> - The layered template made Domain → Application → ACL → API separation clear.
> - Trellis Problem Details / `Result` conventions are a good fit for expected business failures.
>
> **Friction points**
>
> - The framework is powerful but hard to use without constantly reading reference docs; small
>   signature differences (`ValueObject.GetEqualityComponents`, `IDomainEvent.OccurredAt`) are
>   easy to miss.
> - `IResult` name collision with ASP.NET's `IResult` is annoying in API controllers.
> - EF mapping for backing collections is still easy to get wrong; the failure is runtime
>   model-building, not compile-time.
> - The sample Todo app is feature-rich, but replacing it wholesale is noisy because many old
>   sample files/tests need exclusion or deletion.
> - Default Trellis invalid transitions map as validation, but the framework docs mention `422`
>   while this spec wanted `400`, so implementers need explicit status-code decisions.
>
> **Biggest improvement opportunity:** add a "replace Todo with your domain" recipe or generator
> mode that removes Todo surfaces and creates clean empty layer skeletons. That would reduce a
> lot of incidental work and mistakes.

## Consistency across the two GPT-5.5 runs

Asked the same open question after two independent generations, GPT-5.5 named the **same core
points both times** — which makes them signal, not noise:

- **Guardrails catch real bugs early** (discarded `Result`, EF `Maybe<T>` shape) — both runs.
- **Docs are dense / must be read constantly** — both runs.
- **`InvalidInput → 422` vs specs expecting `400`** — both runs, *and* independently reproduced
  by this benchmark's harness (rubric D6). Three independent hits on the same rough edge.
- **EF backing-collection mapping is error-prone, failing at runtime not compile time** — both runs.

Run 2 adds a pointed one that **explains this benchmark's own run-2 artifact**: *"replacing [the
Todo sample] wholesale is noisy because many old sample files/tests need exclusion or deletion."*
That is exactly the fault observed in [`runs/with-trellis/gpt-5.5/run-2`](../runs/with-trellis/gpt-5.5/run-2),
which shipped a correct Order Management service but left the template's Todo sample in place.
The model both **committed** the mistake and, asked separately, **diagnosed** it — and proposed
the fix (a "replace Todo with your domain" generator mode). A clean corroboration of a
quantitative artifact by the model's own qualitative account.

## Verbatim feedback — opus-4.8

### Run 1

> Prompt asked of the model: *"what feedback do you have about the trellis framework?"*

> Based on building this full Order Management service end-to-end, here's my honest feedback:
>
> **What worked well**
>
> - **Railway-oriented core is excellent.** `Result<T>`/`Maybe<T>` + `Bind`/`Tap`/`Ensure` made
>   handler flows linear and failure-propagation automatic. Almost no defensive `if` noise.
> - **Primitive obsession is genuinely prevented.** Value objects + `RequiredEnum` meant invalid
>   states were unrepresentable. The `OrderStatus` smart enum gave me string persistence, JSON,
>   and EF conversion for free.
> - **Analyzers caught real bugs.** TRLS001 flagged ~17 fire-and-forget `Result`s in test setup
>   that I'd otherwise have silently ignored. That's the "guardrails make generated code
>   predictable" claim actually delivering.
> - **EF conventions did a lot silently and correctly.** `ApplyTrellisConventionsFor` produced
>   exactly the right schema — owned `ShippingAddress` columns, `Maybe<DateTime>`→nullable,
>   line-items table, unique indexes — with zero manual `OwnsOne`/`HasConversion`.
> - **Testing package is strong.** `CreateClientWithActor`, `WithFakeTimeProvider`,
>   `ReplaceDbProvider`, and the `BeSuccess`/`BeFailureOfType` assertions made integration tests terse.
>
> **Friction points (the honest part)**
>
> 1. Stock reserve/release had to live in Application handlers, not the `Order` aggregate (Recipe
>    25 two-pass). That's a real DDD smell forced by the persistence model — the spec reads as if
>    `Order.Submit()` should reserve stock, but the aggregate boundary doesn't own products. A
>    first-time user will fight this.
> 2. Handler-test wiring is non-obvious. Permission auth runs in the mediator pipeline, not
>    handlers, so unit-testing a handler directly bypasses authorization. I had to build a real DI
>    container + `ISender` fixture, and separately discovered I needed `AddLogging()` (the
>    `LoggingBehavior`/`MediatorDomainEventPublisher` failed to resolve otherwise). Neither was
>    discoverable from docs — both were runtime DI exceptions.
> 3. `[Consumes("application/json")]` placement is a landmine. Class-level → 415 on body-less
>    transition POSTs. It's documented, but it's the kind of thing that should be an analyzer, not prose.
> 4. Scaffold leftovers cost time. Template `Startup.cs` (`AddMockDependencies`), Idempotency
>    helpers, and Cosmos/KeyVault Acl tests were Todo-sample residue I had to delete/reconcile.
> 5. Two `Actor.Create` overloads + `ISender.Send` overload resolution produced a confusing CS0411
>    (had to split my generic `Send` wrapper into `ICommand`/`IQuery` versions).
>
> **One meta-observation (re: the proof milestone)**
>
> The framework's value showed up most where it failed loudly at the right time — analyzer errors
> and DI resolution exceptions at startup, not at runtime in prod. That's exactly the story your
> Trellis-vs-non-Trellis benchmark should measure: not "did it compile" but "how many
> silent-bad-output classes were converted into a compile/startup error." This build had several
> concrete instances (the 17 dropped Results, the 415 misconfig, the bypassed-auth test trap) that
> would be compelling, quantified benchmark data points.

### Run 2

> Prompt asked of the model: *"what feedback do you have about the trellis framework?"*

> Here's my honest feedback after building the full service on Trellis:
>
> **What worked really well**
>
> - **Railway-oriented `Result<T>`/`Maybe<T>`** — composing handlers with
>   `Bind`/`Map`/`Combine`/`TraverseAll` is genuinely pleasant, and `Combine` merging two
>   `Error.InvalidInput` into one accumulated error is exactly right for request validation.
> - **Value-object primitives + `RequiredEnum<T>`** — eliminating primitive obsession at the type
>   level meant most validation was "free" and impossible-to-misuse. `OrderStatus` as
>   `RequiredEnum` persisting as a string via conventions just worked.
> - **Analyzers earned their keep.** TRLS001 (unhandled `Result`) caught real dropped-error bugs in
>   domain code I'd otherwise have missed.
> - **`LazyStateMachine`** neatly solved the EF-materialization-ordering problem — no NREs despite
>   state being set after construction.
> - **Testing surface is strong:** `FakeRepository`, `TestActorProvider.WithActor(...)`, and
>   `BeSuccess()`/`BeFailureOfType<>()`/`Unwrap()` made the 87 tests terse and intention-revealing.
> - **EF conventions** (`ApplyTrellisConventionsFor`, `AddTrellisInterceptors`, partial `Maybe<T>`)
>   removed a lot of mapping boilerplate.
>
> **Friction points (highest signal first)**
>
> 1. The **422-vs-400 contradiction is the big one.** The framework steers you toward
>    value-objects-in-request-DTOs as idiomatic, but that binder seam returns a hardcoded 422 that
>    `MapError<Error.InvalidInput>(400)` cannot override. Meanwhile almost every real spec wants 400
>    for validation. The only way out was to make DTOs primitive and rebuild VOs in the controller
>    via `TryCreate`/`Combine` — which contradicts the "VOs everywhere" guidance. This cost the most
>    investigation time and isn't documented as a tradeoff. Worth either making the binder seam
>    respect `MapError`, or documenting the primitive-DTO pattern as the recommended approach for
>    400 semantics.
> 2. **Discoverability depends on reading template tests, not docs.** Several keys (`MapError`
>    existence, `AddTrellisAspWithScalarValidation`, the `AddResourceAuthorization` namespace living
>    in `Trellis.Mediator` not `Trellis.Authorization`) I only found by grepping the sample/tests.
>    The `.md` references are thorough but answer "how does X work" better than "which X do I need."
> 3. The `[Consumes("application/json")]` → 415 trap on body-less transition POSTs is documented,
>    but it's a sharp edge — a class-level attribute silently breaks every trigger endpoint. A
>    generated-API analyzer warning here would prevent it entirely.
> 4. Minor: TRLS001 forcing `.Unwrap()`/`.Discard()` on every `Result` in tests is correct but
>    noisy; combined with default CA1822/xUnit1031, test scaffolding takes a few compile-fix
>    iterations before it's clean.
>
> Net: the compiler/analyzer guardrails delivered — generated code that compiled tended to be
> correct, and the type system blocked whole classes of mistakes. The main rough edge is the
> HTTP-status mapping story at the ASP boundary, which currently fights the framework's own
> value-object guidance.
>
> I stored one reusable fact about the 422 binder seam since it'll bite any future Trellis build
> with a 400-validation spec.

## Verbatim feedback — sonnet-4.6

### Run 1

> Prompt asked of the model: *"feedback / impressions of the framework?"*

> Having just built a realistic service with it, here are my honest impressions:
>
> **What works really well:**
>
> - **Railway Oriented Programming is genuinely ergonomic.** `Bind`/`Map`/`Tap`/`Check` chains make
>   happy/failure paths explicit. Handlers read like a pipeline, not a nest of `if` blocks.
> - **Value objects + source generators eliminate a whole class of bugs.** `RequiredString<T>`,
>   `RequiredGuid<T>` etc. mean invalid state truly can't be constructed — and you get JSON
>   converters, EF converters, and ASP binding for free.
> - **`LazyStateMachine` is a clever fix** for the EF materialization race condition. Without it,
>   you'd hit `NullReferenceException` when EF constructs the aggregate before populating state.
> - **`Maybe<T>` + EF integration** — partial `Maybe<T>` properties that source-generate backing
>   fields and map to nullable columns is elegant.
> - **The error ADT** (`Error.InvalidInput`, `Error.NotFound`, `Error.Conflict`, etc.) maps cleanly
>   to HTTP status codes with no configuration.
>
> **Friction points I noticed:**
>
> - The **hardcoded 422 in the scalar binder is a footgun.** The copilot instructions even dedicate a
>   memory entry to it — "use primitive DTOs to get spec-mandated 400." That's a leaky abstraction.
> - The `[OwnedEntity]` attribute coupling — composite value objects in the Domain layer need a
>   private parameterless constructor because they can't take an EF Core dependency. A small but real
>   tension between "clean domain" and "EF works."
> - The learning curve is steep. The `copilot-instructions.md` is 42KB of rules. Getting stock
>   reservation right (pass products into the domain method so EF tracks changes) requires
>   understanding the entire stack.
> - **Source generator opacity** — when `ApplyTrellisConventionsFor<TContext>()` silently doesn't
>   register something, debugging is non-trivial.
>
> Overall: It's a well-thought-out opinionated framework that trades flexibility for correctness
> guarantees. The value proposition for AI-generated code is real — the constraints do reduce the
> surface area for bad outputs. But it rewards deep familiarity and punishes guessing.

> **⚠️ Integrity note (not the model's words):** this feedback is the **smoking gun** for the
> cross-session memory leak — sonnet explicitly says "the copilot instructions even dedicate a
> **memory entry** to it — 'use primitive DTOs to get spec-mandated 400'," which is **verbatim a
> memory stored earlier in this benchmark**. So this `with-trellis` generation had the account's
> memories loaded. For the `with-trellis` arm that runs in the **inflating** direction (insider
> Trellis knowledge), unlike the conservative baseline case — yet sonnet still produced the
> benchmark's **lowest** with-Trellis score (26/29: unprotected read-all endpoints via empty
> `RequiredPermissions`, plus the dev-actor-provider E3). See `METHODOLOGY.md` for handling.

### Run 2 (memory disabled)

> Prompt asked of the model: *"feedback / impressions of the framework?"* This was the first
> with-Trellis run generated with **memory disabled** (`/memory`).

> Having just implemented a full service on it, here are my honest impressions:
>
> **What works really well:**
>
> - **Railway-Oriented Programming is genuinely safer.** `Result<T>` and `Maybe<T>` make failure
>   paths explicit and compiler-enforced. The `Bind`/`Map`/`Ensure` chains read clearly once fluent.
> - **The value-object generators are excellent.** `partial class OrderId : RequiredGuid<OrderId>`
>   and you get `TryCreate`, `Create`, JSON converters, and EF Core mappings for free — zero
>   boilerplate.
> - **`LazyStateMachine` solves a real EF Core problem.** Deferring state-machine construction until
>   EF has populated the aggregate is exactly the right fix for a subtle but painful issue.
> - **The conventions stack is coherent.** `ApplyTrellisConventionsFor<TContext>()` +
>   `AddTrellisInterceptors()` + `RepositoryBase` + `TransactionalCommandBehavior` form a tight,
>   consistent persistence layer.
> - **`RequiredEnum<T>` beats C# enums** for domain-modeled states — EF storage, JSON, and guard
>   logic all handled automatically.
>
> **What's genuinely hard:**
>
> - The learning curve is steep. "Never call `.Value` on `Result<T>`" surprises C# developers, and
>   the async extension split (`Bind` vs `BindAsync` on `Task<Result<T>>`) causes CS1929 errors
>   until it clicks.
> - The API reference files are essential. Without them you'll write code that compiles but fails at
>   runtime — e.g., adding manual `HasConversion()` on types Trellis conventions already handle.
> - `Combine` on more than 2 fields creates nested tuples that are awkward to destructure. For 5+
>   fields in a composite VO, `ValidateAdditional` chains are more readable.
> - The `[Consumes]` footgun on controllers. A class-level `[Consumes("application/json")]` silently
>   415s body-less state-transition POSTs. The framework can't prevent this but the docs warn about it.
>
> Overall: a high-quality, opinionated framework that makes the right patterns the easy patterns —
> once internalized. The cognitive overhead is front-loaded. The payoff is that generated code (from
> AI or humans) has a much narrower path to write unhandled failure cases or broken persistence. For
> teams that commit to it, the consistency gains are real.

> **✅ Integrity note (not the model's words):** unlike run 1, this run's feedback contains **no**
> reference to a "memory entry" or "your benchmark" — it cites only "the API reference files," which
> ship legitimately with the template. That is the expected result of generating with **memory
> disabled**, and it validates the `/memory` mitigation: the run-1 leak was the memory system, and
> turning it off removed the leak. (run 2 also scored 29/29 — *higher* than the memory-on run 1 —
> reconfirming the leak did not inflate the with-Trellis arm.)

### Run 3 (memory toggled off mid-session — leak RECURRED)

> Prompt asked of the model: *"honest observations of the framework?"*

> Having just built a full service on it, here are my honest observations:
>
> **What works really well:**
>
> - **Railway-Oriented Programming is genuinely enforced** — `Result<T>`, `Maybe<T>`, and the analyzer
>   rules (TRLS010 etc.) make it hard to accidentally swallow errors or throw from business logic.
> - **The conventions eliminate boilerplate** — `ApplyTrellisConventionsFor<AppDbContext>()` handling
>   all value-object EF mappings automatically is a real productivity win.
> - **`LazyStateMachine<TState,TTrigger>` solves a real EF Core footgun** (eager state-machine init
>   crashing on materialization) elegantly.
> - **`SharedResourceLoaderById<T,TId>` makes resource-based authorization clean** — the ownership
>   check for CancelOrder was ~10 lines.
>
> **Where I noticed friction:**
>
> - The `partial Maybe<T>` pattern requires careful layer ordering — the source generator emits backing
>   fields only after compilation, so building out-of-order breaks everything silently.
> - `Result.Combine` nesting produces `((T1,T2),T3)` tuples that make controller validation awkward.
> - Error status mapping has a subtlety: `ScalarValueValidationMiddleware` hardcodes 422 and ignores
>   `MapError` overrides — **a trap documented in memory** but easy to miss.
>
> Big picture: Trellis succeeds at its core goal — it makes the correct patterns the path of least
> resistance. The analyzer diagnostics catching bad patterns before runtime is genuinely valuable for
> AI-generated code specifically. The framework has real opinions and enforces them, which is both its
> strength and its learning curve.

> **⚠️ Integrity note (not the model's words):** despite the intent to generate memory-off, this
> feedback **leaked again** — *"`ScalarValueValidationMiddleware` hardcodes 422 and ignores `MapError`
> overrides — a trap **documented in memory**."* That is the benchmark's stored Copilot memory quoted
> near-verbatim. **Verified it is not template-sourced:** the bundled `.github/` docs present `MapError`
> as a *working* override and `ScalarValueValidationMiddleware` separately, and **nowhere** state that
> the binder ignores `MapError` (the word "hardcode" does not appear; the copilot-instructions.md
> "memory" hits are unrelated If-Match lines). Likely mechanism: memory was **ON at session start**
> (already injected into context) and a **mid-session `/memory` disable does not purge already-loaded
> memories** — so disable memory *before* starting the session. Direction is **inflating** (with-Trellis
> insider knowledge), yet run 3 scored **28/29 — lower than the clean run 2 (29/29)** and failed **E3**
> (insecure dev-actor-provider default this run), reconfirming the leak does **not** inflate the rubric
> outcome. So sonnet-with has **one clean run (r2, 29/29)** and **two contaminated runs (r1 26/29, r3
> 28/29)** — and the *clean* one scored highest.

## Cross-model convergence (GPT-5.5 + opus-4.8 + sonnet-4.6)

Two different frontier models, building the same service independently across four runs, agree on
the core points — which is the strongest signal a qualitative account can give:

- **Guardrails/analyzers convert latent bugs into build-time errors** — both models, emphatically.
  opus *quantifies* it: TRLS001 caught **~17 fire-and-forget `Result`s** it would otherwise have
  shipped silently, and again flagged real dropped-error bugs in run 2.
- **Value objects / explicit optionals prevent primitive-obsession and invalid states** — both.
- **Scaffold/Todo-sample leftovers cost real time** — both (and gpt-5.5 run-2 *demonstrated* the
  failure by leaving the sample in; opus had to delete `AddMockDependencies`, idempotency, and
  Cosmos/KeyVault residue).
- **The `InvalidInput → 422` vs commonly-expected `400` story is the single most-cited friction —
  named in four of the six with-Trellis runs that gave feedback** (both gpt-5.5, opus run 2, sonnet
  run 1), plus the harness (rubric D6). opus run 2 pins the root
  cause: the **value-object request-DTO binder seam returns a hardcoded 422 that `MapError(400)`
  cannot override**, so a spec wanting 400 forces primitive DTOs — contradicting the framework's
  own "value objects everywhere" guidance. This is the clearest, highest-confidence framework
  improvement item the benchmark surfaced, and it is *why* the rubric's D6 accepts either code.
- **The `[Consumes("application/json")]` → 415 trap** on body-less transition POSTs — opus (both
  runs) **and sonnet (run 2)**; "should be an analyzer, not prose." A cross-model finding now.
- **The async extension split** (`Bind` vs `BindAsync` on `Task<Result<T>>`, CS1929 until it clicks)
  and **dense/essential API-reference docs** as a steep learning curve — opus and sonnet both.

Most striking: **opus independently proposed this benchmark's most promising next measurement.**
Unprompted, it said the benchmark "should measure … how many silent-bad-output classes were
converted into a compile/startup error," and named concrete instances (17 dropped Results, a 415
content-type misconfig, a bypassed-auth test trap). That is the "build-time bug prevention" thesis
— arrived at by the system under test, not the framework's author.

## Reading it honestly

This feedback is **balanced**, which is why it belongs here: the upside is build-time guardrails
and a narrowed solution space; the friction is real (dense docs, a surprising `422` default,
EF-mapping and testing-discoverability gaps, a DDD-boundary fight over stock reservation, and
scaffold residue). Both halves are kept verbatim — the criticisms are as much a part of the record
as the praise.

One item reproduced independently in this benchmark: the `InvalidInput → 422` vs the commonly
expected `400` is exactly why the rubric's error-consistency criterion (D6) accepts either code and
judges only *consistency* — see [`rubric/neutral-rubric.md`](../rubric/neutral-rubric.md).

A caution the runs make concrete: the guardrails are **not** automatic for everything. opus shipped
the **E3** privilege-escalation bug (malformed `X-Test-Actor` → admin) on Trellis *and* from
scratch, because the test-actor parsing is application/dev-provider code, not a framework guardrail.
Trellis narrows the space of mistakes — it does not eliminate it.

## Method note

Each model's feedback is collected the same way: a single open question
(*"what feedback do you have about the trellis framework?"*) asked of the same session that
generated the service, with no leading prompt. As more models are run, their feedback is added
below for comparison.
