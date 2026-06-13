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

## Reading it honestly

This feedback is **balanced**, which is why it belongs here: points 1 and 6 are the upside
(build-time guardrails, a narrowed solution space); points 2–5 are real friction (dense docs, a
surprising `422` default, EF-mapping and testing-discoverability gaps). Both halves are kept
verbatim — the criticisms are as much a part of the record as the praise.

One of these reproduced independently in this benchmark: the `InvalidInput → 422` vs the
commonly-expected `400` (point 3) is exactly why the rubric's error-consistency criterion (D6)
accepts either code and judges only *consistency* — see
[`rubric/neutral-rubric.md`](../rubric/neutral-rubric.md).

## Method note

Each model's feedback is collected the same way: a single open question
(*"what feedback do you have about the trellis framework?"*) asked of the same session that
generated the service, with no leading prompt. As more models are run, their feedback is added
below for comparison.
