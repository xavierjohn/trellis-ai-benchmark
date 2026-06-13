# Methodology

A benchmark is only worth as much as its ability to survive a hostile read. This document
states the design, the controls, and — most importantly — the ways it could still be wrong.

## 1. The question

> Holding the model and the task fixed, does adopting **Trellis** (its project template,
> framework, and bundled AI guidance) change the **quality of the service an AI generates** —
> measured by outcomes a reviewer or a production incident would care about, not by whether
> the code "looks like Trellis"?

## 2. Design

A 2 × 3 × 3 factorial, fully crossed:

| Factor | Levels |
|---|---|
| **Condition** | `without-trellis` (baseline), `with-trellis` (treatment) |
| **Model** | `gpt-5.5`, `opus-4.8`, `sonnet-4.6` |
| **Run** | `1`, `2`, `3` (repeats, to expose run-to-run variance) |

= **18 independently generated services**, all kept verbatim under [`runs/`](runs/).

Every service is produced from the **same** specification —
[`spec/order-management.md`](spec/order-management.md), a deliberately framework-agnostic
functional spec (14 endpoints, a six-state order lifecycle, role+resource authorization, an
explicit error→status contract, a required test suite). The spec names no framework, library,
or pattern.

## 3. What the two conditions actually are

This is the part a skeptic should pin down, so it is stated plainly.

- **`without-trellis` (baseline).** The model is told to build the service from scratch with
  *whatever production-grade stack it judges best* — and explicitly **not** to use Trellis.
  The prompt actively encourages a strong implementation so the baseline is a real senior-team
  effort, **not a strawman**. ([`prompts/without-trellis.md`](prompts/without-trellis.md))
- **`with-trellis` (treatment).** The model scaffolds with `dotnet new trellis-asp` and builds
  on the framework, following the guidance bundled in the template's `.github/`.
  ([`prompts/with-trellis.md`](prompts/with-trellis.md))

The two prompts are **identical** except for those framework instructions — same spec, same
deliverable, same constraints (`.NET 10`, `TimeProvider`, no test-only shortcuts in production,
a passing test suite, `/health`).

So the treatment is honestly **"the whole Trellis package"** — template + framework + its
AI-facing docs — versus a competent from-scratch baseline. That is exactly the decision a team
faces ("should we adopt this?"), and it is the comparison the result speaks to. It does **not**
attempt to isolate the framework from its template or its guidance; those are inseparable in
real use.

## 4. Neutral scoring (the anti-circularity rule)

The [rubric](rubric/neutral-rubric.md) is the heart of the credibility argument. Its 30
criteria score **only observable outcomes**:

- HTTP status codes and the spec's error→status contract,
- security behavior (permission enforcement, the cancel-ownership rule, *no* silent
  privilege-escalation on a malformed actor, *no* stack-trace leakage),
- business behavior (lifecycle, stock reserve/release, totals, line-item rules),
- whether it builds, boots, and ships passing tests.

No criterion rewards `Result<T>`, value objects, analyzers, or any Trellis mechanism. A
hand-written baseline that enforces authorization and returns the right status codes scores
exactly the same as a Trellis service that does. **If the rubric mentioned Trellis, the
benchmark would measure adherence to Trellis, not quality — and would prove nothing.**

## 5. The harness

Scoring is automated and mostly **black-box** ([`harness/`](harness/)):

- A **probe** boots each service and drives it over HTTP — including adversarial requests it
  cannot fake compliance for (a malformed `X-Test-Actor` must not act as admin; a non-owner
  must not cancel another actor's order). Functional probes run in **Development** (the spec
  mandates it — `EnsureCreated` and the connection string are gated to Development).
- The **no-leak** check (E4) is run separately against a **Production** boot: ASP.NET's
  developer-exception page leaks stack traces by framework default in Development, so testing
  there would flag a default both arms share rather than a real, code-level vulnerability.
  In Production, a leak means the service deliberately serializes internals to clients.
- **Static checks** confirm it builds and that its own test suite runs and passes.

Black-box-by-construction is the strongest evidence available: the probe does not know or care
which framework produced the service.

## 6. Controls and fairness measures

- **One spec, one rubric, one probe** applied identically to all 18 services.
- **Prompts differ in exactly one dimension** (framework provision); diff them to verify.
- **Three runs per cell** surface nondeterminism instead of cherry-picking one lucky generation.
- **Raw artifacts are committed** — every generated service is in `runs/` for inspection; the
  result is reproducible, not asserted.
- **`NuGetAudit` is disabled at build time** so a transitive-dependency CVE published *after*
  generation can't fail a build for reasons unrelated to the model's output.
- **The probe was validated against a known-good implementation** (it scored 29/30, every
  behavioral criterion green) — a control proving the probe passes correct code.

## 7. Threats to validity (and what we did about them)

| Threat | Mitigation / honest caveat |
|---|---|
| **Circularity** — scoring Trellis idioms. | Rubric scores outcomes only; no criterion references any framework. §4. |
| **Strawman baseline** — hobbling "without". | The baseline prompt demands a strong, production-grade implementation and free library choice. It is the same task, not a worse one. §3. |
| **Treatment is a bundle** — can't separate framework from template/guidance. | Acknowledged explicitly. The claim is about *adopting Trellis as shipped*, which is the real decision. Not a claim about any single mechanism. §3. |
| **Spec leakage** — the spec hinting at Trellis. | The spec was scrubbed of framework-leaning language (no CQRS mandate, neutral glossary, no framework references). It is committed for inspection. |
| **Small N** — 3 runs × 3 models. | We report per-run results and variance, not just a mean; all raw runs are public. This is an existence-and-direction study, not a p-value. |
| **Prompt-author bias.** | Both prompts are committed and differ by one paragraph; anyone can re-run with their own wording. |
| **Probe gaps / false negatives.** | The probe is validated against a correct implementation and its evidence strings are recorded per criterion for audit. |
| **Model/version drift over time.** | Each run records model, template version, and SDK in `meta.json`; re-running later is expected to differ and that's disclosed. |
| **Generation contamination** — an arm reading the other's files or the framework source. | Each service is generated in isolation and committed as-is; the generation method is recorded in `meta.json`. |
| **Environment-dependent leakage** — flagging a dev-only default as a vulnerability. | The no-leak check (E4) is run in **Production**, not Development, because ASP.NET's developer-exception page leaks by default in Development for *every* service. Only a Production leak — deliberately serialized internals — is scored. §5. |
| **Mid-benchmark spec revision** — changing the spec could look like tuning toward a framework. | On 2026-06-12 the spec's validation→status mapping was corrected from `400` to `422` (RFC 9110 §15.5.21, the correct status for semantic validation; `400` is kept for malformed/missing-version requests). This is **score-neutral**: the rubric accepts *either* `400` or `422` for validation (it always has), so the change rewards no framework and **invalidates no completed run** — the 9 runs generated against the `400` mapping returned `400`, which the rubric still accepts. Each run's `meta.json` records the exact `spec_sha256`/`prompt_sha256` it was generated against, so the provenance of every run is auditable. The revision's only effect is to stop forcing implementations to fight an arguably-incorrect `400` mandate — a friction the benchmark itself surfaced (see `findings/model-feedback.md`). |
| **Baseline "Trellis-awareness"** — the without-Trellis prompt originally named Trellis. | The first **5** baseline runs used a prompt that said "Do not use the Trellis framework or the Trellis ASP template" to define the condition — so the model knew Trellis by name (and, being public, it is in training data regardless). The clean-room still prevented the **material** contamination (reading Trellis's source/docs from disk), and the effect is **conservative** (awareness can only strengthen the baseline). The prompt was then **de-named** for the remaining baseline runs — it now excludes "a pre-built domain/service template" and "an opinionated DDD / railway-oriented result framework" without naming Trellis. Each run's `meta.json` `prompt_sha256` records which version it used; the two paste prompts still differ in exactly one block (the framework provision). Disclosed in `findings/baseline-self-assessments.md`. |
| **Cross-session leakage via shared memory / global instructions** — the clean-room isolates the *filesystem*, not the account's *memory*. | Copilot **user-scoped memories** (and global instruction files under `$HOME/.copilot/`) load into *every* session for the same user — including clean-room generations — and a generation session can even *write* memories that load into later ones. **Proven, not inferred:** (a) the de-named `without-trellis/sonnet-4.6/run-2` session's two user turns contained no "Trellis", yet the model referenced "the Trellis version" and "your benchmark" (a brand-new benchmark, absent from training data); and (b) `with-trellis/sonnet-4.6/run-1`'s feedback **quotes a benchmark-stored memory verbatim** — "the copilot instructions even dedicate a memory entry to it — *use primitive DTOs to get spec-mandated 400*." So both arms' completed runs ran with memories loaded. **Crucially, the direction differs by arm and is favourable to skepticism either way:** for the *baseline* it is **conservative** (awareness can only strengthen the baseline, biasing against Trellis); for the *with-Trellis* arm it is **inflating** (insider Trellis knowledge), which means the **measured with-Trellis scores are an upper bound** on the true (clean) value — so the observed "Trellis is at parity-or-behind on the rubric" cannot be an artifact *hiding* a Trellis advantage. The one fact actually shown to have leaked (`422`→`400`) has **zero** score impact (the rubric accepts both), and the dominant input to a with-Trellis build is the template's own bundled guidance regardless. **Mitigation:** memory and global instructions are the **two** leak vectors and are disabled by **two** controls — memory via `/memory` off (it persists across sessions; prompt mode `-p` is off by default), and the global `$HOME/.copilot/copilot-instructions.md` separately. **The arms differ:** for *without-trellis* we launch `--no-custom-instructions` (blanket — no legitimate Trellis instructions to lose); for *with-trellis* we must **keep** the template's project-local `.github/copilot-instructions.md` (it is the framework guidance under test), so we do **not** pass the blanket flag and instead suppress only the global file (temporarily renamed, or toggled off via `/instructions`), verified with `/env`. **Confirmed effective — but only when memory is off *before* the session starts:** the first memory-off run, `with-trellis/sonnet-4.6/run-2`, cited only the template's bundled `.github/trellis-api-*.md` docs with **no** "memory entry"/"your benchmark" reference (unlike memory-on run-1) — *and* scored 29/29, **higher** than contaminated run-1 (26/29). **However**, `with-trellis/sonnet-4.6/run-3` leaked **again** despite memory being toggled off: its feedback quotes the stored memory near-verbatim ("`ScalarValueValidationMiddleware` hardcodes 422 and ignores `MapError` … a trap documented in memory"), a claim verified *not* to be derivable from the bundled docs. The operator had found memory **on** and disabled it **mid-session** — and a mid-session `/memory` disable does **not** purge memories already injected at session start. **Protocol fix:** ensure `/memory` is off and then **start a fresh session** (the toggle persists), confirming via `/env` at startup that **no** memories/global instructions are loaded *before* pasting the prompt. The direction remains **inflating** for with-Trellis, and run-3 again scored **lower** than the clean run-2 (28/29 vs 29/29) — so even the recurring leak does not inflate the arm. Contaminated runs are **kept and disclosed**, not re-generated, because the contamination is conservative for the headline. Per-run `meta.json`/session provenance is recorded. |

## 8. What this proves — and what it doesn't

- **It can show**: under identical task and model, whether adopting Trellis moves measurable,
  framework-neutral quality outcomes, and by how much, across three frontier models with
  run-to-run variance visible.
- **It does not show**: that Trellis is the only way to reach those outcomes (a disciplined
  team can hit every criterion by hand), nor that the effect generalizes beyond this spec, nor
  anything about long-term maintenance, performance, or developer experience.
- **A known blind spot**: the rubric scores the *final* working service, so it cannot see the
  mistakes Trellis converted into build-time errors before that state was reached. On a fully
  specified spec with a thorough test suite those bugs are often caught anyway; on a real,
  under-specified, evolving codebase they would not be. The generating models' own accounts of
  this mechanism are recorded — verbatim, with their criticisms intact — in
  [`findings/model-feedback.md`](findings/model-feedback.md). That is qualitative context, not
  scored evidence, and is kept strictly separate from the rubric.

## 9. Reproduce

```bash
# 1. generate the 18 services from the two prompts (see prompts/ and runs/README.md)
# 2. score everything
cd harness && pip install -r requirements.txt
python run_all.py --all
python aggregate.py
# 3. read results/summary.md and results/criteria-matrix.md
```
