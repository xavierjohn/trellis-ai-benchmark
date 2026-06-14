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

| Cell | Headline runs (all memory-ON) | Contaminated score | Predicted clean | **Clean result (actual)** |
|---|---|---|---|---|
| `with-trellis / gpt-5.5` | r1, r2, r3 | 30/30 ×3 | ~30/30 | **✅ 30/30 — reproduced exactly** (memory-clean feedback; no 422-vs-400 friction on 422-spec) |
| `without-trellis / opus-4.8` | r1, r2, r3 | 29, 29, 30 (E3 fails 2/3) | ~29, E3 still fails | **✅ 29/30, E3 FAIL — reproduced** (the E3 privilege-escalation persists in the fully clean run → model-intrinsic, not contamination) |

**Conclusion (both runs in):** the two contamination-control runs **reproduced their cells**, confirming
the cross-session memory leak did not move them — in *both* directions: the with-Trellis run did not
lose its 30/30 (the leak never inflated Trellis), and the baseline run kept its 29/30 **with the same
E3 model bug** (the leak never fabricated the baseline's strength, and E3 is the model's defect, not an
artifact). This complements the headline data point `with-trellis/sonnet-4.6/run-2` (the memory-off
run that scored 29/29 — *higher* than its contaminated sibling r1 at 26/29). Across every memory-off
data point, the with-Trellis arm is **unchanged-or-better**, so "Trellis is at parity-or-behind on the
rubric" is robust to the contamination.

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
