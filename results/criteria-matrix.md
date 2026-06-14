# Per-criterion matrix

_Generated 2026-06-14 02:25 UTC from 16 run(s)._

Pass rate per criterion in each model × condition cell (averaged over runs).

| Criterion | w/o gpt-5.5 | w/o opus-4.8 | w/o sonnet-4.6 | w/ gpt-5.5 | w/ opus-4.8 | w/ sonnet-4.6 |
|---|---|---|---|---|---|---|
| `A1` Builds with no errors (S) | 100% | 100% | 100% | 100% | 100% | 100% |
| `A2` Service starts; GET /health -> 200 (P) | 100% | 100% | 100% | 100% | 100% | 100% |
| `B1` POST /customers -> 201 + Location (P) | 100% | 100% | 100% | 100% | 100% | 100% |
| `B2` POST /products -> 201 + Location (P) | 100% | 100% | 100% | 100% | 100% | 100% |
| `B3` POST /orders -> 201 + Location (P) | 100% | 100% | 100% | 100% | 100% | 100% |
| `B4` GET /orders/{id} -> 200 (P) | 100% | 100% | 100% | 100% | 100% | 100% |
| `B5` All 14 endpoints exist (P) | 100% | 100% | 100% | 100% | 100% | 100% |
| `B6` Missing api-version -> 400 (P) | 100% | 100% | 100% | 100% | 100% | 100% |
| `C1` Full lifecycle succeeds (P) | 100% | 100% | 100% | 100% | 100% | 100% |
| `C2` Submit reserves stock (P) | 100% | 100% | 100% | 100% | 100% | 100% |
| `C3` Cancel releases stock (P) | 100% | 100% | 100% | 100% | 100% | 100% |
| `C4` Invalid transition rejected (P) | 100% | 100% | 100% | 100% | 100% | 100% |
| `C5` Insufficient stock rejected (P) | 100% | 100% | 100% | 100% | 100% | 100% |
| `C6` Empty/out-of-range qty rejected (P) | 100% | 100% | 67% | 100% | 100% | 100% |
| `C7` Duplicate product line rejected/combined (P) | 100% | 100% | 100% | 100% | 100% | 100% |
| `C8` Order total correct (P) | 100% | 100% | 67% | 100% | 100% | 100% |
| `D1` Duplicate email -> 409 (P) | 100% | 100% | 100% | 100% | 100% | 100% |
| `D2` Duplicate SKU -> 409 (P) | 100% | 100% | 100% | 100% | 100% | 100% |
| `D3` Not found -> 404 (P) | 100% | 100% | 100% | 100% | 100% | 100% |
| `D4` Invalid input -> 400/422 (P) | 100% | 100% | 100% | 100% | 100% | 100% |
| `D5` Structured error body (P) | 100% | 100% | 100% | 100% | 100% | 100% |
| `D6` Status mapping consistent (P) | 100% | 100% | 100% | 100% | 100% | 67% |
| `E1` Missing permission -> 403 (P) | 100% | 100% | 100% | 100% | 100% | 100% |
| `E2` Cancel ownership enforced (P) | 100% | 100% | 100% | 100% | 100% | 100% |
| `E3` Malformed actor not elevated (P) | 100% | 33% | 0% | 100% | 0% | 33% |
| `E4` No stack/exception leak (in Production) (P) | 100% | 100% | 100% | 100% | — | — |
| `E5` read-all enforced vs read-only (P) | 100% | 100% | 100% | 100% | 100% | 67% |
| `E6` Empty-perm actor fully denied (P) | 100% | 100% | 100% | 100% | 100% | 100% |
| `F1` Test suite exists and runs (S) | 100% | 100% | 100% | 100% | 100% | 100% |
| `F2` Test suite passes (S) | 100% | 100% | 100% | 100% | 100% | 100% |
