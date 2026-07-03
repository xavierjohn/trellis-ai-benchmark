# Brownfield proofs

Self-contained before/after conversions that show, on **one real slice at a time**, what changes
when a messy endpoint is rebuilt on Trellis. The claim is deliberately narrow and measurable:
*for this operation*, conversion removes specific, reproducible defects and shrinks the review
surface — the "brutal brownfield" half of *proof over surface area*.

Each proof pairs a **runnable** legacy implementation (with tests that pass by *reproducing* its
defects) against the Trellis reference implementation scored in the [Trellis training lab](https://github.com/xavierjohn/Trellis-training) (the unmodified `after/OrderManagement/`).

This complements the generation benchmark in this repo: the [`runs/`](../runs/) experiment measures
whether models reach a spec-compliant service **with vs. without** Trellis when building *from scratch*;
these proofs measure what conversion of *existing, messy* code removes.

| Proof | Operation | Defects removed |
|---|---|---|
| [`submit-order/`](submit-order/) | Submit Order (`Draft → Submitted`, reserve stock) | oversell race · partial-reservation corruption · business-failure-as-500 · missing state guard · missing authorization |

> See each proof's `README.md` for the run commands, the defect-by-defect table, and the
> review-surface comparison.
