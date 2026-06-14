# Results — Trellis AI Benchmark

A narrative read of the numbers. The tables here are summarized from the machine-generated
[`results/summary.md`](results/summary.md) and [`results/criteria-matrix.md`](results/criteria-matrix.md)
(regenerated from the raw runs by `harness/aggregate.py`, never hand-edited). Every claim links to
the artifact that backs it.

> **One-line finding.** On a complete, well-specified task, three frontier models reach a
> spec-compliant, secure-in-production service **with or without** Trellis — the outcome rubric shows
> **parity (97% vs 97%)**. Trellis's measurable value is not a higher score on a finished, fully-tested
> service; it is **converting a class of likely code-generation mistakes into build-time errors** —
> which the rubric, by design, cannot see, but the generating models repeatedly describe.

---

## 1. Headline: outcome parity

Mean pass rate over the 30 framework-neutral criteria (3 runs per cell, 18 services total):

| Model | Without Trellis | With Trellis | Δ (pts) |
|---|---|---|---|
| gpt-5.5 | 99% | 99% | **−0** |
| opus-4.8 | 98% | 97% | **−1** |
| sonnet-4.6 | 96% | 95% | **−0** |
| **All models** | **97%** | **97%** | **±0** |

With Trellis, every model lands **tie-or-fractionally-behind** — none ahead — and the aggregate is a
tie. **No honest reading of the outcome rubric shows Trellis meaningfully ahead or behind.** That is
the intended, credible result — and the reason the rubric is scored on outcomes only (a rubric that
rewarded "uses `Result<T>`" would measure adherence to Trellis, not quality).

## 2. The rubric is nearly saturated — and *where* it isn't is the interesting part

**24 of the 30 criteria pass 100% in all six model × condition cells** — build, all 14 endpoints, the
full lifecycle, stock reserve/release, the error contract, structured error bodies, and the test
suites are correct essentially everywhere, in both arms. The entire signal lives in six criteria
([full matrix](results/criteria-matrix.md)):

| Criterion | Where it drops | Cause | Framework-preventable? |
|---|---|---|---|
| **E3** — malformed `X-Test-Actor` must not elevate to admin | w/o opus 33%, w/o sonnet 0%, **w/ gpt 67%, w/ opus 0%, w/ sonnet 33%**, w/o gpt 100% | The §5.5 test-actor header is parsed by **application / dev-provider** code; several models default a malformed header to an admin actor (a real privilege-escalation). On Trellis the usual cause is wiring `AddDevelopmentActorProvider` with a default-admin actor. | **No.** It fails *on Trellis too* — the parsing seam is app / dev-provider code, not a framework guardrail. **Model-dependent**, and (tellingly) gpt only started failing it once it built **idiomatically** with Trellis (see below). |
| **E4** — no internal leak in Production | N/A for w/ opus, w/ sonnet | Their Trellis services refuse to boot in Production without real auth wired (a deliberate framework guard), so the no-leak check can't run and is excluded from the denominator. | This is Trellis being *stricter*, not a failure. |
| **C6** — empty/out-of-range line items rejected at create | w/o sonnet 67% | One sonnet baseline run (`without/sonnet/r3`) creates an **empty draft order** and only adds items via a separate endpoint, so create-time line-item validation is absent. | A spec-deviation in one from-scratch run. |
| **E2** — cancel ownership enforced | w/o gpt 67% | One gpt baseline run (`without/gpt/r3`) **deterministically hangs** on owner self-cancellation and wedges the server (an effective DoS). | A genuine runtime reliability bug — see §4. |
| **D6 / E5** — consistent status mapping / read-all enforced | w/ sonnet 67% each | One sonnet *Trellis* run (`with/sonnet/r1`) left read-all endpoints unprotected (empty `RequiredPermissions`) and mapped a status inconsistently. | A real authz bug — Trellis did **not** prevent it here (the permission list was simply left empty). |

The dominant differentiator is **E3**, and it lives in **application / dev-provider code, not a
framework guardrail** — so it fails *on Trellis too*. The sharpest illustration came from re-checking
idiom usage (see §7): the original `with-trellis/gpt-5.5/run-1` had scaffolded the template but written
largely *plain C#* (manual, secure actor parsing) and scored a clean **30/30**. Re-generated to build
**idiomatically** with Trellis, the *same model on the same task* scored **28/29** — it now **fails
E3** (the idiomatic `AddDevelopmentActorProvider` defaults a malformed actor to admin) and **E4 becomes
N/A** (the framework's production-auth guard refuses to boot without real auth). In other words,
adopting Trellis *idiomatically* here **cost** gpt rubric points rather than winning them. That is a
striking, honest data point: it both reinforces the parity headline and shows the value (the prod-auth
guard, the explicit dev-actor seam) is about *structure and strictness*, not a higher score.

## 3. Consistency: Trellis steadies the *structure*, not the *score*

"Are the three runs in a cell consistent?" depends on *which* consistency — and it is **not** uniform
across models ([`harness/consistency.py`](harness/consistency.py)):

| cell | run scores | rate range | `.cs` files (r1/r2/r3) | projects | idiom |
|---|---|---|---|---|---|
| with / gpt-5.5 | 28/29, 30/30, 30/30 | 96.6–100 | 51 / 82 / 38 | 8/8/8 | idiomatic |
| with / opus-4.8 | 28/29 ×3 | **96.6 (flat)** | 81 / 84 / 73 | 8/8/8 | idiomatic |
| with / sonnet-4.6 | 26/29, 29/29, 28/29 | **89.7–100** | 120 / 126 / 92 | 8/8/8 | idiomatic |
| without / gpt-5.5 | 30/30, 30/30, 29/30 | 96.7–100 | **8 / 2 / 4** | 2/2/2 | — |
| without / opus-4.8 | 29/30, 29/30, 30/30 | 96.7–100 | 43 / 38 / 48 | 5/7/5 | — |
| without / sonnet-4.6 | 29/30, 29/30, 28/30 | 93.3–96.7 | 40 / 31 / 43 | **2 / 5 / 7** | — |

- **Structural consistency → Trellis wins decisively.** Every with-Trellis run ships the *same
  8-project layered skeleton* (the template imposes it). Without Trellis, architecture is free-form —
  **sonnet alone shipped 2, 5, and 7 projects** across its three baseline runs; gpt stayed minimal
  (always 2). Trellis makes the skeleton uniform.
- **…but code volume still varies within it.** gpt-with ranged **38 → 82 `.cs` files** — the template
  fixes the *structure*, not how much the model writes inside it.
- **Score consistency → model-driven, not arm-driven.** The steadiest cell in the whole study is
  **opus with-Trellis (28/29 ×3, zero variance)**; the most variable is **sonnet with-Trellis
  (89.7–100)**. Both arms otherwise show ~3-point run-to-run dips from a single run-specific bug.
- **Idiom consistency → not uniform across models.** opus and sonnet built idiomatically in **all
  three** runs; **gpt did not** (1 of 3 came out `template-only`, since re-generated — see §7).
  Handing a model the template does not guarantee uniform adoption.

**Takeaway:** Trellis substantially improves **architectural** consistency (uniform skeleton, mostly-
uniform idioms); **outcome-score** consistency is governed by the *model* (opus steadiest, sonnet most
variable), roughly the same with or without the framework. (n=3 per cell — directional, not a variance
statistic.)

## 4. What the rubric can't see — and why it matters

The rubric scores the *final, working, fully-tested* service. On a complete spec with a thorough test
suite, many latent bugs get caught **before** scoring — by the model's own tests — so they never
surface as a failed criterion. That is precisely the gap Trellis targets, and the benchmark caught two
vivid examples on the **baseline** side that a real, under-specified, evolving codebase might not:

- **gpt-5.5 — strongest model, perfect 30/30 in both arms on runs 1–2 — shipped a server-wedging
  hang.** Its third baseline service deterministically **deadlocks on owner self-cancellation**;
  every subsequent request times out (a from-scratch DoS). Caught only because the black-box probe
  exercised the owner path. **All three of its *Trellis* runs were clean.** (n=1, but exactly the
  class of subtle concurrency/resource bug that structured persistence is meant to make harder.)
  See [`runs/without-trellis/gpt-5.5/run-3/meta.json`](runs/without-trellis/gpt-5.5/run-3/meta.json).
- **opus-4.8 — baseline — hit and fixed an EF Core graph-state trap** (a client-generated child key
  makes EF emit a 0-row `UPDATE` → 500). It debugged it off a live instance and resolved it
  cleanly — *thorough testing surfaced it in the baseline too.* The Trellis arm meets the same class
  of EF child-collection difficulty through conventions instead. **Both arms pay an EF tax, at
  different seams** — an honest wash, not a clean Trellis win.

The generating models, asked for an unguided assessment immediately after building, converge on the
same mechanism ([`findings/model-feedback.md`](findings/model-feedback.md)):

- **Railway-oriented `Result<T>`/`Maybe<T>` + analyzers make failure paths compiler-enforced.** opus
  *quantified* it — the `TRLS001` "unhandled `Result`" analyzer flagged ~17 fire-and-forget results
  (in test setup) and caught real dropped-error bugs it would otherwise have missed.
- **Value objects + source-generated conventions** remove a class of primitive-obsession and
  EF-mapping bugs — "invalid state truly can't be constructed."
- The cost is a **steep, documentation-dependent learning curve** and **rigidity** — "you build it
  Trellis's way or you fight it."

Trellis's own summary, in the models' words: it **narrows the path to writing unhandled failure cases
or broken persistence** — most valuable exactly where a test suite *isn't* exhaustive.

## 5. A finding that cuts the other way: the most-cited friction is spec-dependent

Across the feedback, the single most-named rough edge was *"Trellis maps validation to **422**, but my
spec wanted **400**."* The benchmark's spec was later revised to map validation to **422** (RFC 9110
§15.5.21 — the modern-correct choice). On the 422 spec, **two independent models (opus and gpt)
reported getting "the whole 422/404/409/403 contract almost for free" and did not cite the friction at
all.** So that complaint is not a framework flaw — it is the cost of *disagreeing* with a defensible
default. (The rubric accepts either `400` or `422` for validation, so this never moved a score.) There
is still a genuine underlying sharp edge worth fixing — the value-object request-DTO binder hardcodes
422 and ignores `MapError(400)` — filed as [`findings/framework-issue-422-binder-seam.md`](findings/framework-issue-422-binder-seam.md).

## 6. Cost: Trellis is markedly more expensive to generate

Mean tokens to produce one working service ([`results/summary.md`](results/summary.md)). `output`
(generated work) is the most comparable signal; `input` is dominated by the identical, mostly-cached
spec.

| Model | Without: output | With: output | With ÷ Without (output) | Without: total | With: total |
|---|---|---|---|---|---|
| gpt-5.5 | 28.0k | 40.8k | 1.5× | 2.23M | 7.21M |
| opus-4.8 | 77.1k | 177.5k | 2.3× | 8.11M | 36.14M |
| sonnet-4.6 | 59.4k | 184.1k | 3.1× | 5.16M | 16.92M |

Building **on Trellis costs ~1.5–3.1× the output tokens** (and ~3.2–4.5× total), because the model must
read the bundled API references, follow the layered structure, and satisfy the analyzers. This is the
real, measured trade-off: **more generation cost now, in exchange for a narrower path to latent bugs
later.** Whether that trade is worth it depends on how well-tested and how long-lived the code is.

## 7. Honesty about the experiment (threats to validity)

Full treatment in [`METHODOLOGY.md`](METHODOLOGY.md); the load-bearing ones:

- **Cross-session memory leakage.** Some early runs were generated with the CLI's cross-session
  *memory* enabled, and a few feedback texts quoted a stored note verbatim. Disclosed, not hidden. The
  bias direction is favorable to skepticism either way (conservative for the baseline; *inflating* for
  with-Trellis — so the measured with-Trellis score is an **upper bound**). Two memory-off
  **contamination-control runs** ([`validation/`](validation/), excluded from the headline) close the
  loop: `with/gpt` reproduced **30/30** and `without/opus` reproduced **29/30 with the same E3 bug**.
  Across *every* memory-off data point the with-Trellis arm is **unchanged-or-better**, so "Trellis is
  at parity-or-behind on the rubric" is robust to the contamination.
- **The harness is the same for both arms.** Five probe-fairness fixes were made *during* scoring,
  every one because a probe assumption (e.g. a payload field name, the address shape, enum-serialized-
  as-int, the default actor) was about to fail a spec-correct service for the wrong reason — each
  verified against the actual source, each arm-agnostic, and a final full re-score confirmed all 18
  results come from one probe version.
- **Adoption ≠ idiomatic use.** Because the rubric scores outcomes, not idioms, it can't tell whether
  a with-Trellis run actually *used* the framework. A separate, **non-scored** diagnostic
  ([`harness/idiom_check.py`](harness/idiom_check.py), in every with-Trellis `meta.json`) classifies
  each run by its use of value objects, smart enums/state machine, and the Mediator command pipeline
  (the source-generated package Trellis uses, *not* MediatR). It flagged the original
  `with-trellis/gpt-5.5/run-1` as `template-only` (template scaffolded, then largely plain C#); that
  run was **re-generated** into a genuinely idiomatic Trellis build, so **all 10** with-Trellis services
  are now idiomatic. The rebuild scored **28/29** — *lower* than the plain-C# original's 30/30 (see §2)
  — which reinforces the parity finding. It failed the *condition*, not the *score*; the diagnostic is
  what made that auditable instead of invisible.
- **n is small** (3 runs × 3 models × 2 arms). This is a credibility study, not a powered statistical
  claim. The artifacts are all here to re-score, re-run, or disagree.

## 8. Read the evidence yourself

- Per-run scores and provenance: each `runs/<arm>/<model>/run-N/result.json` + `meta.json`
- The exact black-box checks: [`harness/probe.py`](harness/probe.py)
- The models' own words: [`findings/model-feedback.md`](findings/model-feedback.md),
  [`findings/baseline-self-assessments.md`](findings/baseline-self-assessments.md)
- Reproduce: `cd harness && python run_all.py --all && python aggregate.py`

**Bottom line.** For a fully-specified task with good tests, Trellis does not change the *outcome
score* — frontier models can hit the spec either way. Its value is upstream of the rubric: it makes the
correct patterns the default and turns a class of would-be runtime bugs into build-time errors, at a
real cost in generation tokens and learning curve. That is a defensible, honest case — and it is the
case the numbers actually support, neither more nor less.
