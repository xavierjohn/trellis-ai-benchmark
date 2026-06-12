"""Copy a clean-room-generated service from a scratch dir into its runs/ slot.

Usage:
  python intake.py <scratch_dir> --condition with-trellis --model opus-4.8 --run 1

Copies source only (skips bin/obj/.git/.vs/*.db and the like) and preserves the slot's
existing meta.json. The generated code is otherwise copied verbatim — never edit it.
"""

from __future__ import annotations

import argparse
import shutil
from pathlib import Path

HERE = Path(__file__).resolve().parent
REPO = HERE.parent
CONDITIONS = {"with-trellis", "without-trellis"}
MODELS = {"gpt-5.5", "opus-4.8", "sonnet-4.6"}

SKIP_DIRS = {"bin", "obj", ".git", ".vs", ".vscode", "node_modules", ".scratch", ".idea"}
SKIP_SUFFIX = {".db", ".db-shm", ".db-wal", ".user", ".suo"}
SKIP_NAMES = {"meta.json", ".gitkeep", ".DS_Store", "Thumbs.db"}


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("scratch_dir")
    ap.add_argument("--condition", required=True, choices=sorted(CONDITIONS))
    ap.add_argument("--model", required=True, choices=sorted(MODELS))
    ap.add_argument("--run", required=True, type=int, choices=[1, 2, 3])
    ap.add_argument("--force", action="store_true", help="overwrite existing source in the slot")
    args = ap.parse_args()

    src = Path(args.scratch_dir).resolve()
    if not src.is_dir():
        ap.error(f"scratch dir not found: {src}")
    dest = REPO / "runs" / args.condition / args.model / f"run-{args.run}"
    dest.mkdir(parents=True, exist_ok=True)

    existing = [p for p in dest.iterdir() if p.name not in SKIP_NAMES]
    if existing and not args.force:
        ap.error(f"{dest} already has content; pass --force to overwrite")

    copied = 0
    for path in src.rglob("*"):
        rel = path.relative_to(src)
        if any(part in SKIP_DIRS for part in rel.parts):
            continue
        if path.is_dir():
            continue
        if path.suffix in SKIP_SUFFIX or path.name in SKIP_NAMES:
            continue
        target = dest / rel
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(path, target)
        copied += 1

    gk = dest / ".gitkeep"
    if gk.exists():
        gk.unlink()
    print(f"Copied {copied} file(s) -> {dest.relative_to(REPO)}")
    print("Next: fill generated_at/notes in that slot's meta.json, then run:")
    print(f"  python run_all.py ../runs/{args.condition}/{args.model}/run-{args.run}")


if __name__ == "__main__":
    main()
