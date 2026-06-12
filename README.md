# Trellis AI Benchmark

**Does adopting [Trellis](https://github.com/xavierjohn/Trellis) change the quality of the
service an AI generates?** This repo answers that with a controlled, reproducible experiment
and **framework-neutral** scoring — every claim is backed by raw, inspectable artifacts.

The same task is handed to three frontier models, twice each: once building **with** Trellis
(its template + framework + AI guidance) and once building a strong implementation **without**
it. Eighteen generated services are then scored by an automated black-box harness on outcomes
only — correct status codes, enforced authorization, no privilege escalation, no stack leakage,
passing tests — **never** on whether the code uses any Trellis idiom.

> The neutrality is the whole point. A benchmark that rewarded "uses `Result<T>`" would measure
> adherence to Trellis, not quality. This one measures only what a reviewer or a production
> incident would care about. See **[METHODOLOGY.md](METHODOLOGY.md)**.

## Headline result

See **[`results/summary.md`](results/summary.md)** for the generated table (mean pass rate over
the 30 neutral criteria, per model, with the with-Trellis delta) and
**[`results/criteria-matrix.md`](results/criteria-matrix.md)** for the per-criterion breakdown.

> Results are regenerated from the raw runs by `harness/aggregate.py`; they are not hand-edited.

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
harness/              Python scorer: probe.py, static_checks.py, run_all.py, aggregate.py
results/              generated headline + per-criterion tables
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

## License

[MIT](LICENSE). The generated services under `runs/` are AI output preserved verbatim as
evidence.
