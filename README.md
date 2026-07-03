# Trellis AI Benchmark

**Does adopting [Trellis](https://github.com/xavierjohn/Trellis) change the quality of the
service an AI generates?** This repo answers that with a controlled, reproducible experiment
and **framework-neutral** scoring — every claim is backed by raw, inspectable artifacts.

The same task is handed to three frontier models in two conditions, **three runs each**: building
**with** Trellis (its template + framework + AI guidance) and building a strong implementation
**without** it. The resulting **eighteen** generated services (3 models × 2 conditions × 3 runs) are
then scored by an automated black-box harness on outcomes only — correct status codes, enforced
authorization, no privilege escalation, no stack leakage, passing tests — **never** on whether the
code uses any Trellis idiom.

> The neutrality is the whole point. A benchmark that rewarded "uses `Result<T>`" would measure
> adherence to Trellis, not quality. This one measures only what a reviewer or a production
> incident would care about. See **[METHODOLOGY.md](METHODOLOGY.md)**.

## Headline result

Mean pass rate over the 30 framework-neutral criteria (3 runs per cell, 18 services):

| Model | Without Trellis | With Trellis | Δ (pts) |
|---|---|---|---|
| gpt-5.5 | 99% | 99% | −0 |
| opus-4.8 | 98% | 97% | −1 |
| sonnet-4.6 | 96% | 95% | −0 |
| **All models** | **97%** | **97%** | **±0** |

**Outcome parity.** On this well-specified task, frontier models reach a spec-compliant,
secure-in-production service **with or without** Trellis. 24 of the 30 criteria pass 100% in *every*
cell; the few differences are model-specific (the recurring one — a malformed-header privilege
escalation — fails on Trellis too, because it lives in application code).

→ **[`RESULTS.md`](RESULTS.md)** is the full narrative: what the parity does and doesn't mean, the
build-time-bug-prevention thesis with concrete evidence (a from-scratch server-wedging hang, an EF
graph-state trap), the spec-dependent 422 finding, generation-cost numbers, and the contamination
disclosure. The raw tables are **[`results/summary.md`](results/summary.md)** and
**[`results/criteria-matrix.md`](results/criteria-matrix.md)**.

> Results are regenerated from the raw runs by `harness/aggregate.py`; they are not hand-edited.

**The outcome rubric is only half the story.** It scores the *final* working service — so when a
frontier model reaches a spec-compliant service both with and without Trellis, the rubric shows
parity. What it cannot see is the wrong turns that never reached the final state. For that, the
generating model's own first-person account is recorded in
**[`findings/model-feedback.md`](findings/model-feedback.md)** — including its summary that
Trellis's value is "narrowing the space of acceptable code so generated implementations fail
loudly instead of becoming subtle runtime bugs."

## How it works

```
            same spec ──────────────┬──────────────── same rubric, same probe
                                     │
   prompts/without-trellis.md ─▶  ✗ Trellis  ─▶ runs/without-trellis/<model>/run-{1,2,3}/ ─┐
   prompts/with-trellis.md    ─▶  ✓ Trellis  ─▶ runs/with-trellis/<model>/run-{1,2,3}/   ─┤
                                     │                                                      ▼
                                     └────────────▶  harness (build · boot · HTTP probe · tests)
                                                                        │
                                                                        ▼
                                                          results/summary.md  (+ matrix)
```

- **Identical inputs.** Both arms get the same [spec](spec/order-management.md). The two
  [prompts](prompts/) differ in exactly one paragraph: whether to use Trellis. Diff them.
- **Neutral rubric.** 30 outcome-only criteria across build/run, API surface, business
  behavior, error contract, security, and tests — each tied to a spec section.
  ([`rubric/neutral-rubric.md`](rubric/neutral-rubric.md))
- **Automated black-box scoring.** The harness boots each service and probes it over HTTP,
  including adversarial security checks, then runs its test suite.
  ([`harness/`](harness/))

## Repository layout

```
spec/                 the single, framework-agnostic spec given to every model
prompts/              the two generation prompts (identical but for the framework)
rubric/               the 30 outcome-only scoring criteria
runs/                 the 18 raw generated services (+ per-run result.json)
validation/           2 memory-off contamination-control runs (NOT in the headline aggregate)
harness/              Python scorer: probe.py, static_checks.py, run_all.py, aggregate.py
results/              generated headline + per-criterion tables
RESULTS.md            the narrative read of the numbers (start here for findings)
findings/             qualitative, non-scored evidence: model framework feedback + baseline self-assessments
brownfield/           before/after conversion proofs (one messy endpoint rebuilt on Trellis)
GENERATION.md         clean-room runbook for producing the 18 services
METHODOLOGY.md        design, controls, and threats to validity
```

## Reproduce

Requires the **.NET 10 SDK** and **Python 3.11+**.

```bash
cd harness
pip install -r requirements.txt
python run_all.py --all     # build → boot → probe → test every run
python aggregate.py         # roll up into results/
```

To regenerate the services, follow the clean-room runbook in
[`GENERATION.md`](GENERATION.md): each is produced in isolation from the self-contained paste
file in [`prompts/paste/`](prompts/paste/) (prompt + spec, nothing else), then dropped into the
matching `runs/<condition>/<model>/run-N/` folder. See [`runs/README.md`](runs/README.md) for
the folder convention and the per-run `meta.json` audit schema.

## Brownfield proofs

The generation benchmark above asks whether Trellis changes AI output when building a service *from
scratch* — and finds outcome parity on this well-specified task. [`brownfield/`](brownfield/) asks the
complementary question about *existing* code: take one messy endpoint, convert a single slice to
Trellis, and measure what changes. Each proof is a **runnable** legacy implementation whose tests pass
by *reproducing* its defects, paired against the scored Trellis reference — a narrow, concrete take on
"proof over surface area."

## License

[MIT](LICENSE). The generated services under `runs/` are AI output preserved verbatim as
evidence.
