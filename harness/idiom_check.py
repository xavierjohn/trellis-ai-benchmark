"""Non-scored Trellis idiom-usage diagnostic.

The headline rubric is outcome-only and framework-neutral — it deliberately does NOT reward using
Trellis idioms. This separate, advisory diagnostic answers a different question: when a model was
handed the Trellis template, did it actually *build with the framework*, or did it scaffold the
template and then write largely plain C#? "Adoption ≠ idiomatic use." It NEVER affects a score; it
is written to each with-trellis run's meta.json under "trellis_idiom_diagnostic" purely for audit.

Usage:
  python idiom_check.py ../runs/with-trellis/gpt-5.5/run-1          # print diagnostic
  python idiom_check.py --write-all                                 # write into every with-trellis meta.json
"""

from __future__ import annotations

import argparse
import json
import re
from pathlib import Path

HERE = Path(__file__).resolve().parent
REPO = HERE.parent

# Idiom markers (the framework's core "MUST" surfaces, per the template copilot-instructions).
IDIOM = {
    "value_objects": re.compile(r":\s*Required(?:String|Guid|Int|Long|Decimal|Double|DateTime|DateOnly|Uri|Email|Bool)\w*\s*<"),
    "required_enum": re.compile(r":\s*RequiredEnum\s*<"),
    "aggregates": re.compile(r":\s*Aggregate\s*<"),
    "entities": re.compile(r":\s*Entity\s*<"),
    "state_machine": re.compile(r"\bLazyStateMachine\s*<"),
    "mediatr_handlers": re.compile(r"\bI(?:Command|Query|Request)Handler\s*<"),
    "mediatr_dispatch": re.compile(r"\b_sender\.Send\b|\bISender\b|\bIMediator\b"),
    "authorize": re.compile(r"\bIAuthorize\b|\bIAuthorizeResource\s*<"),
    "maybe": re.compile(r"\bMaybe\s*<"),
    "result": re.compile(r"\bResult\s*<|\bResult\.(?:Ok|Fail|Success|Failure|Combine|Ensure)\b"),
    "ef_conventions": re.compile(r"\bApplyTrellisConventionsFor\b|\bAddTrellisInterceptors\b"),
}
# Anti-idioms (plain-C# patterns the framework's rules steer away from on domain surfaces).
ANTI = {
    "plain_enums": re.compile(r"\bpublic\s+enum\s+\w"),
    "throws": re.compile(r"\bthrow\s+new\b"),
}


def _cs_files(run_dir: Path):
    for p in run_dir.rglob("*.cs"):
        if any(seg in ("bin", "obj") for seg in p.parts):
            continue
        yield p


def analyze(run_dir: Path) -> dict:
    counts = {k: 0 for k in IDIOM}
    anti = {k: 0 for k in ANTI}
    n_files = 0
    for f in _cs_files(run_dir):
        n_files += 1
        text = f.read_text(encoding="utf-8", errors="ignore")
        for k, rx in IDIOM.items():
            counts[k] += len(rx.findall(text))
        for k, rx in ANTI.items():
            anti[k] += len(rx.findall(text))

    # Classification on the framework's load-bearing surfaces: value objects, smart enums / state
    # machine, and the MediatR/CQRS pipeline. Result<T> alone (which even a near-plain service uses
    # for validation) is NOT sufficient to count as idiomatic.
    uses_value_objects = counts["value_objects"] > 0
    uses_smart_state = counts["required_enum"] > 0 or counts["state_machine"] > 0
    uses_pipeline = counts["mediatr_handlers"] > 0
    core = sum([uses_value_objects, uses_smart_state, uses_pipeline])
    if core == 3:
        verdict = "idiomatic"
    elif core == 0:
        verdict = "template-only"  # scaffolded the template but wrote largely plain C#
    else:
        verdict = "partial"

    return {
        "note": "NON-SCORED advisory. Does the with-trellis service actually build WITH the framework "
                "(value objects, smart enums/state machine, MediatR pipeline) vs. scaffold the template "
                "and write plain C#? Never affects the neutral rubric score.",
        "verdict": verdict,
        "uses": {
            "value_objects": uses_value_objects,
            "smart_enum_or_state_machine": uses_smart_state,
            "mediatr_pipeline": uses_pipeline,
        },
        "idiom_counts": counts,
        "anti_idioms": anti,
        "cs_files": n_files,
    }


def _write_meta(run_dir: Path) -> str:
    meta_path = run_dir / "meta.json"
    if not meta_path.exists():
        return f"SKIP (no meta.json): {run_dir}"
    meta = json.loads(meta_path.read_text(encoding="utf-8"))
    meta["trellis_idiom_diagnostic"] = analyze(run_dir)
    meta_path.write_text(json.dumps(meta, indent=2) + "\n", encoding="utf-8")
    d = meta["trellis_idiom_diagnostic"]
    return f"{run_dir.relative_to(REPO)}: {d['verdict']:13} (VO={d['uses']['value_objects']}, smart={d['uses']['smart_enum_or_state_machine']}, pipeline={d['uses']['mediatr_pipeline']})"


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("run_dir", nargs="?")
    ap.add_argument("--write-all", action="store_true",
                    help="write the diagnostic into every with-trellis run's meta.json (runs/ + validation/)")
    args = ap.parse_args()

    if args.write_all:
        targets = []
        for root in ("runs", "validation"):
            base = REPO / root / "with-trellis"
            if base.exists():
                targets += [d for d in sorted(base.glob("*/*")) if (d / "meta.json").exists()]
        for d in targets:
            print(_write_meta(d))
    elif args.run_dir:
        print(json.dumps(analyze(Path(args.run_dir).resolve()), indent=2))
    else:
        ap.error("provide a run_dir or --write-all")


if __name__ == "__main__":
    main()
