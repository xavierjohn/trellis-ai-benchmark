# `validation/` — contamination-control runs (NOT part of the headline)

These are **memory-clean repeats** of cells whose headline runs were all generated with Copilot
**memory ON** (the cross-session leak documented in [`../METHODOLOGY.md`](../METHODOLOGY.md)). They
exist to answer one question:

> When a cell is regenerated under the corrected, fully isolated clean-room procedure, does it
> reproduce the same rubric score as the contaminated headline runs?

## Why a separate tree (and not `runs/.../run-4`)

`harness/aggregate.py` rolls **`runs/**`** into the headline 3-run cell averages. Dropping a 4th
run into `runs/` would (a) skew those two cells to a 4-run average and (b) fold a validation run
into the published headline. Keeping these under **`validation/`** — a sibling of `runs/` that the
aggregate never globs — leaves the headline as the **original, disclosed-contaminated, conservative**
3×3 matrix. Verified: `python aggregate.py` still reports exactly 13 results with this tree present.

## Which cells, and why only these two

After the 5 remaining headline runs are generated (all memory-clean), **4 of the 6** condition×model
cells already contain ≥1 clean run. Only two cells are 100% contaminated with no clean run planned:

| Cell | Headline runs (all memory-ON) | Contaminated score | What a clean repeat should show |
|---|---|---|---|
| `with-trellis / gpt-5.5` | r1, r2, r3 | 30/30 ×3 | ~30/30 — gpt passes E3 in both arms; should reproduce trivially |
| `without-trellis / opus-4.8` | r1, r2, r3 | 29, 29, 30 (E3 fails 2/3) | ~29 — opus's malformed-actor→admin (E3) is **model-intrinsic**, not memory-driven, so it should persist clean |

A reproduced score is positive evidence the leak did not move these cells. (We already have one such
data point in the headline itself: `with-trellis/sonnet-4.6/run-2`, the first memory-off run, scored
29/29 — *higher* than its contaminated sibling r1 at 26/29.)

## How to generate, intake, and score

1. **Generate** per [`../GENERATION.md`](../GENERATION.md) step 1, using the **per-arm isolation**:
   - `without-trellis/opus-4.8`: `copilot --no-custom-instructions --model opus-4.8` + `/memory` off
     (blanket flag is safe — no legitimate Trellis instructions exist).
   - `with-trellis/gpt-5.5`: memory off, but **keep** the template's project
     `.github/copilot-instructions.md`; suppress only the global `$HOME/.copilot/...` file (rename it
     or `/instructions`-toggle), verified with `/env`. Do **not** use the blanket flag here.
2. **Intake** into this tree (note `--root validation`):
   ```powershell
   cd harness
   python intake.py <scratch_dir> --condition with-trellis    --model gpt-5.5  --run 1 --root validation
   python intake.py <scratch_dir> --condition without-trellis --model opus-4.8 --run 1 --root validation
   ```
3. **Score** by explicit path (the harness writes `result.json` into the slot):
   ```powershell
   python run_all.py ../validation/with-trellis/gpt-5.5/run-1
   python run_all.py ../validation/without-trellis/opus-4.8/run-1
   ```
4. **Fill** `generated_at` + `tokens` in each `meta.json` (the `role` field is already set to
   `contamination-control`).

## How these are reported

Separately from the headline — as a short clean-vs-contaminated comparison (this README's table,
filled with the observed clean scores). They are **never** folded into the headline averages. The
clean runs use the current 422-spec, which is rubric-neutral (criteria D4/C4/C5/C6 accept 400 **or**
422), so the comparison holds on the rubric rate; the only intended changed variable is memory on→off.
