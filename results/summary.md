# Headline results

_Generated 2026-06-13 03:06 UTC from 6 run(s)._

Each cell is the **mean pass rate over the 30 neutral criteria**, averaged across runs.
Higher is better. Δ is the with-Trellis advantage in percentage points.

| Model | Without Trellis | With Trellis | Δ (pts) |
|---|---|---|---|
| gpt-5.5 | 100% | 100% | +0 |
| opus-4.8 | 97% | — | — |
| sonnet-4.6 | — | — | — |
| **All models** | **98%** | **100%** | **+2** |

### Generation cost (tokens per service)

Mean tokens to produce one working service. `output` is the generated work — the most
comparable signal across arms, since `input` is dominated by the identical pasted spec
(most of which is cached).

| Model | Without: output | Without: total | With: output | With: total |
|---|---|---|---|---|
| gpt-5.5 | 19.3k | 916.4k | 36.8k | 5.94M |
| opus-4.8 | 75.2k | 6.93M | — | — |
| sonnet-4.6 | — | — | — | — |

> Scores measure **observable outcomes only** (spec compliance, correct status codes,
> security behavior, tests passing) — never the use of any framework. See
> [`rubric/neutral-rubric.md`](../rubric/neutral-rubric.md) and
> [`METHODOLOGY.md`](../METHODOLOGY.md).
