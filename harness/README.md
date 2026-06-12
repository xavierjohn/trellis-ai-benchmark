# Harness

Automated, **framework-neutral** scoring for every generated service. The harness only ever
observes outcomes — it builds each service, boots it, fires HTTP requests (including
adversarial security probes), and runs the service's own test suite. It never inspects *how*
the code is written, so the same 30 criteria apply identically to every implementation.

## Files

| File | Role |
|---|---|
| `criteria.py` | Canonical list of the 30 rubric criteria (id → group, method, label). |
| `probe.py` | Black-box HTTP probe. Talks to a running service; scores the 27 `P` criteria (A2, B*, C*, D*, E*). |
| `static_checks.py` | Build + test-suite checks; scores the 3 `S` criteria (A1, F1, F2). |
| `run_all.py` | Orchestrator: build → boot → probe → test → write `result.json` for one run (or `--all`). |
| `aggregate.py` | Rolls every `result.json` into `../results/summary.md` and `../results/criteria-matrix.md`. |
| `requirements.txt` | Python deps (`requests`). |

## Requirements

- Python 3.11+ and `pip install -r requirements.txt`
- The .NET 10 SDK on `PATH` (`dotnet --version`)

## Run it

```bash
pip install -r requirements.txt

# score one generated service
python run_all.py ../runs/with-trellis/opus-4.8/run-1

# score every populated run directory
python run_all.py --all

# roll the results up into the headline tables
python aggregate.py
```

Each `run_all.py` invocation writes `result.json` into the run directory (all 30 criteria,
`1`/`0`, with evidence) and prints the score. `aggregate.py` reads them all.

## How a run is scored

1. **Build** (`A1`) — `dotnet build -c Release`. `NuGetAudit` is disabled so a *transitive*
   package CVE published after generation can't fail the build (that would make results
   irreproducible); real compiler errors/warnings still fail.
2. **Boot** — the web host's entry-point DLL (found via its `*.runtimeconfig.json`) is launched
   on a free loopback port, HTTP only, `ASPNETCORE_ENVIRONMENT=Development`, with a fresh
   database. The harness polls `/health` until ready (`A2`).
3. **Probe** (`B`–`E`) — `probe.py` auto-detects the service's `api-version`, then drives the
   full spec: CRUD, lifecycle, stock reserve/release (proven behaviorally, since the API
   exposes no product read), the error contract, and the security checks — including the
   adversarial ones (malformed actor must not elevate; no stack-trace leakage).
4. **Test** (`F1`/`F2`) — `dotnet test` on the service's own suite; the summary is parsed for
   both Microsoft.Testing.Platform and VSTest output formats.

## Self-check

The harness was validated against a known-good reference implementation, which scored 29/30
(every behavioral criterion passing). Validating the probe against a correct service is the
control that keeps it honest: if a green implementation didn't pass, the probe would be wrong.

## Notes

- The probe is resilient: a failure in any single check is recorded as a `0` for that criterion
  and never aborts the rest of the run.
- Stock changes are asserted **behaviorally** (reserve → a later submit fails for lack of
  stock; cancel → it then succeeds) because the spec's 14 endpoints include no product-read.
- Where the spec allows a range (e.g. `400` *or* `422` for validation), the probe accepts
  either; see `../rubric/neutral-rubric.md`.
