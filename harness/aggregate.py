"""Roll every runs/**/result.json into the headline tables in ../results/.

Outputs:
  results/summary.md          — the headline: with vs without, per model, with delta
  results/criteria-matrix.md  — every criterion's pass-rate in every model x condition cell
"""

from __future__ import annotations

import json
from collections import defaultdict
from datetime import datetime, timezone
from pathlib import Path

from criteria import ALL_IDS, CRITERIA, GROUPS

HERE = Path(__file__).resolve().parent
REPO = HERE.parent
CONDITIONS = ["without-trellis", "with-trellis"]
MODELS = ["gpt-5.5", "opus-4.8", "sonnet-4.6"]


def load() -> list[dict]:
    out = []
    for rj in (REPO / "runs").glob("**/result.json"):
        try:
            out.append(json.loads(rj.read_text(encoding="utf-8")))
        except Exception:
            pass
    return out


def pct(x: float | None) -> str:
    return "—" if x is None else f"{x*100:.0f}%"


def fmt_k(n: float | None) -> str:
    if n is None:
        return "—"
    return f"{n/1000:.1f}k" if n < 1_000_000 else f"{n/1_000_000:.2f}M"


def token_stats():
    """(cond, model) -> list of token dicts, read from each run's meta.json."""
    data = defaultdict(list)
    for mj in (REPO / "runs").glob("**/meta.json"):
        try:
            m = json.loads(mj.read_text(encoding="utf-8"))
        except Exception:
            continue
        t = m.get("tokens") or {}
        if t.get("total") is None:
            continue
        data[(m.get("condition"), m.get("model"))].append(t)
    return data


def mean_k(lst, field: str) -> str:
    if not lst:
        return "—"
    vals = [t.get(field) for t in lst if t.get(field) is not None]
    return fmt_k(sum(vals) / len(vals)) if vals else "—"


def rates(results: list[dict]):
    """(cond, model, crit) -> mean pass; and (cond, model) -> list of run totals."""
    by_cell_crit = defaultdict(list)   # (cond,model,crit) -> [0/1,...]
    by_cell_run = defaultdict(list)    # (cond,model) -> [run_rate,...]
    for r in results:
        cond, model = r.get("condition"), r.get("model")
        crit = r.get("criteria", {})
        if not cond or not model or not crit:
            continue
        run_passed = 0
        run_total = 0
        for cid in ALL_IDS:
            v = crit.get(cid, {}).get("pass", 0)
            if v is None:          # criterion not applicable for this run (e.g. E4 prod boot)
                continue
            by_cell_crit[(cond, model, cid)].append(v)
            run_passed += v
            run_total += 1
        if run_total:
            by_cell_run[(cond, model)].append(run_passed / run_total)
    return by_cell_crit, by_cell_run


def cell_total(by_cell_run, cond, model):
    vals = by_cell_run.get((cond, model), [])
    return (sum(vals) / len(vals)) if vals else None


def cond_total(by_cell_run, cond):
    vals = [v for (c, m), lst in by_cell_run.items() if c == cond for v in lst]
    return (sum(vals) / len(vals)) if vals else None


def write_summary(by_cell_run, n_results: int):
    lines = [
        "# Headline results",
        "",
        f"_Generated {datetime.now(timezone.utc):%Y-%m-%d %H:%M UTC} from {n_results} run(s)._",
        "",
        "Each cell is the **mean pass rate over the 30 neutral criteria**, averaged across runs.",
        "Higher is better. Δ is the with-Trellis advantage in percentage points.",
        "",
        "| Model | Without Trellis | With Trellis | Δ (pts) |",
        "|---|---|---|---|",
    ]
    for model in MODELS:
        wo = cell_total(by_cell_run, "without-trellis", model)
        wi = cell_total(by_cell_run, "with-trellis", model)
        delta = None if (wo is None or wi is None) else (wi - wo) * 100
        delta_s = "—" if delta is None else f"{delta:+.0f}"
        lines.append(f"| {model} | {pct(wo)} | {pct(wi)} | {delta_s} |")
    wo_all = cond_total(by_cell_run, "without-trellis")
    wi_all = cond_total(by_cell_run, "with-trellis")
    d_all = None if (wo_all is None or wi_all is None) else (wi_all - wo_all) * 100
    lines.append(f"| **All models** | **{pct(wo_all)}** | **{pct(wi_all)}** | "
                 f"**{'—' if d_all is None else f'{d_all:+.0f}'}** |")

    tok = token_stats()
    if tok:
        lines += [
            "",
            "### Generation cost (tokens per service)",
            "",
            "Mean tokens to produce one working service. `output` is the generated work — the most",
            "comparable signal across arms, since `input` is dominated by the identical pasted spec",
            "(most of which is cached).",
            "",
            "| Model | Without: output | Without: total | With: output | With: total |",
            "|---|---|---|---|---|",
        ]
        for model in MODELS:
            wo = tok.get(("without-trellis", model))
            wi = tok.get(("with-trellis", model))
            lines.append(f"| {model} | {mean_k(wo, 'output')} | {mean_k(wo, 'total')} | "
                         f"{mean_k(wi, 'output')} | {mean_k(wi, 'total')} |")

    lines += [
        "",
        "> Scores measure **observable outcomes only** (spec compliance, correct status codes,",
        "> security behavior, tests passing) — never the use of any framework. See",
        "> [`rubric/neutral-rubric.md`](../rubric/neutral-rubric.md) and",
        "> [`METHODOLOGY.md`](../METHODOLOGY.md).",
        "",
    ]
    (REPO / "results" / "summary.md").write_text("\n".join(lines), encoding="utf-8")


def write_matrix(by_cell_crit, n_results: int):
    cells = [(c, m) for c in CONDITIONS for m in MODELS]
    header = "| Criterion | " + " | ".join(
        f"{'w/o' if c=='without-trellis' else 'w/'} {m}" for c, m in cells) + " |"
    sep = "|---|" + "|".join(["---"] * len(cells)) + "|"
    lines = [
        "# Per-criterion matrix",
        "",
        f"_Generated {datetime.now(timezone.utc):%Y-%m-%d %H:%M UTC} from {n_results} run(s)._",
        "",
        "Pass rate per criterion in each model × condition cell (averaged over runs).",
        "",
        header, sep,
    ]
    for cid in ALL_IDS:
        group, method, label = CRITERIA[cid]
        row = [f"`{cid}` {label} ({method})"]
        for c, m in cells:
            vals = by_cell_crit.get((c, m, cid), [])
            row.append(pct(sum(vals) / len(vals)) if vals else "—")
        lines.append("| " + " | ".join(row) + " |")
    lines.append("")
    (REPO / "results" / "criteria-matrix.md").write_text("\n".join(lines), encoding="utf-8")


def main():
    results = load()
    by_cell_crit, by_cell_run = rates(results)
    (REPO / "results").mkdir(exist_ok=True)
    write_summary(by_cell_run, len(results))
    write_matrix(by_cell_crit, len(results))
    print(f"Aggregated {len(results)} result(s) -> results/summary.md, results/criteria-matrix.md")


if __name__ == "__main__":
    main()
