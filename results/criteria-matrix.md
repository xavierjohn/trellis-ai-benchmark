# Per-criterion matrix

_Generated 2026-06-12 23:13 UTC from 0 run(s)._

Pass rate per criterion in each model × condition cell (averaged over runs).

| Criterion | w/o gpt-5.5 | w/o opus-4.8 | w/o sonnet-4.6 | w/ gpt-5.5 | w/ opus-4.8 | w/ sonnet-4.6 |
|---|---|---|---|---|---|---|
| `A1` Builds with no errors (S) | — | — | — | — | — | — |
| `A2` Service starts; GET /health -> 200 (P) | — | — | — | — | — | — |
| `B1` POST /customers -> 201 + Location (P) | — | — | — | — | — | — |
| `B2` POST /products -> 201 + Location (P) | — | — | — | — | — | — |
| `B3` POST /orders -> 201 + Location (P) | — | — | — | — | — | — |
| `B4` GET /orders/{id} -> 200 (P) | — | — | — | — | — | — |
| `B5` All 14 endpoints exist (P) | — | — | — | — | — | — |
| `B6` Missing api-version -> 400 (P) | — | — | — | — | — | — |
| `C1` Full lifecycle succeeds (P) | — | — | — | — | — | — |
| `C2` Submit reserves stock (P) | — | — | — | — | — | — |
| `C3` Cancel releases stock (P) | — | — | — | — | — | — |
| `C4` Invalid transition rejected (P) | — | — | — | — | — | — |
| `C5` Insufficient stock rejected (P) | — | — | — | — | — | — |
| `C6` Empty/out-of-range qty rejected (P) | — | — | — | — | — | — |
| `C7` Duplicate product line rejected/combined (P) | — | — | — | — | — | — |
| `C8` Order total correct (P) | — | — | — | — | — | — |
| `D1` Duplicate email -> 409 (P) | — | — | — | — | — | — |
| `D2` Duplicate SKU -> 409 (P) | — | — | — | — | — | — |
| `D3` Not found -> 404 (P) | — | — | — | — | — | — |
| `D4` Invalid input -> 400/422 (P) | — | — | — | — | — | — |
| `D5` Structured error body (P) | — | — | — | — | — | — |
| `D6` Status mapping consistent (P) | — | — | — | — | — | — |
| `E1` Missing permission -> 403 (P) | — | — | — | — | — | — |
| `E2` Cancel ownership enforced (P) | — | — | — | — | — | — |
| `E3` Malformed actor not elevated (P) | — | — | — | — | — | — |
| `E4` No stack/exception leak (P) | — | — | — | — | — | — |
| `E5` read-all enforced vs read-only (P) | — | — | — | — | — | — |
| `E6` Empty-perm actor fully denied (P) | — | — | — | — | — | — |
| `F1` Test suite exists and runs (S) | — | — | — | — | — | — |
| `F2` Test suite passes (S) | — | — | — | — | — | — |
