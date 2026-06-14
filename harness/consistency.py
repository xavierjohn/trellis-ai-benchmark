"""Per-cell run-to-run consistency report (advisory; not part of the score).

For each model x condition cell, summarizes the spread across its 3 runs along three axes:
  - outcome score (rubric pass-rate range)
  - structure (project count and .cs-file count per run)
  - idiom adherence (with-trellis verdict from trellis_idiom_diagnostic)

The point: Trellis constrains *structure* (the template imposes a fixed skeleton), but run-to-run
*score* variance is driven by the model, not the arm. Numbers feed the "Consistency" section of
RESULTS.md. Run: python consistency.py
"""

from __future__ import annotations

import json
import pathlib
from collections import defaultdict

REPO = pathlib.Path(__file__).resolve().parent.parent


def _run_dirs():
    for rj in sorted((REPO / "runs").glob("*/*/run-*/result.json")):
        yield rj.parent


def main():
    cells = defaultdict(list)
    for d in _run_dirs():
        result = json.loads((d / "result.json").read_text(encoding="utf-8"))
        s = result["score"]
        rate = round(100 * s["passed"] / s["total"], 1)
        cs = [p for p in d.rglob("*.cs") if "bin" not in p.parts and "obj" not in p.parts]
        proj = [p for p in d.rglob("*.csproj") if "bin" not in p.parts and "obj" not in p.parts]
        idiom = ""
        mp = d / "meta.json"
        if mp.exists():
            idiom = json.loads(mp.read_text(encoding="utf-8")).get("trellis_idiom_diagnostic", {}).get("verdict", "")
        parts = d.parts
        cond, model, run = parts[-3], parts[-2], parts[-1]
        cells[(cond, model)].append({"run": run, "passed": s["passed"], "total": s["total"],
                                     "rate": rate, "cs": len(cs), "proj": len(proj), "idiom": idiom})

    hdr = "{:26} {:18} {:12} {:16} {:9} {}".format(
        "cell", "run scores", "rate range", ".cs (r1/r2/r3)", "projects", "idiom")
    print(hdr)
    print("-" * len(hdr))
    for (cond, model), rs in sorted(cells.items()):
        rs.sort(key=lambda x: x["run"])
        scores = " ".join(f"{x['passed']}/{x['total']}" for x in rs)
        rates = [x["rate"] for x in rs]
        rng = f"{min(rates)}-{max(rates)}" if min(rates) != max(rates) else f"{rates[0]} (flat)"
        csf = "/".join(str(x["cs"]) for x in rs)
        proj = "/".join(str(x["proj"]) for x in rs)
        idi = ",".join(sorted({x["idiom"] for x in rs})) if rs[0]["idiom"] else "-"
        print("{:26} {:18} {:12} {:16} {:9} {}".format(
            cond.split("-")[0] + "/" + model, scores, rng, csf, proj, idi))


if __name__ == "__main__":
    main()
