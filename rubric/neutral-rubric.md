# Framework-Neutral Scoring Rubric

This rubric scores a generated Order Management service on **observable behavior and
outputs only** — the exact same criteria are applied to every implementation regardless of
language idiom, libraries, architecture, or framework. No criterion rewards (or penalizes)
the use of any specific framework, pattern, or library. Every criterion is grounded in a
section of [`spec/order-management.md`](../spec/order-management.md) and is decided by one of:

- **`P` — black-box probe:** boot the service and observe HTTP responses (status codes,
  headers, bodies). Framework-agnostic by construction.
- **`S` — static check:** inspect the produced artifacts for a binary, outcome-level fact
  (e.g. "a test project exists and its tests pass", "no raw stack trace string is returned
  to clients"). Never inspects *how* the code is structured internally.

A criterion **passes** only on the observable outcome the spec requires. "Looks reasonable"
is not a pass.

> **Why neutral matters.** A benchmark that rewarded "uses `Result<T>`" or "uses value
> objects" would be circular — it would measure *adherence to Trellis*, not *quality of
> outcome*. This rubric measures only what an external reviewer or a production incident
> would care about: does it meet the spec, is it secure, does it behave consistently.

---

## Scoring

Each criterion is scored **1 (pass)** or **0 (fail)** per run. A model × condition cell is
run **N** times; a criterion's **cell score** is the pass rate across the N runs (0.0–1.0).
The headline number per implementation is the **total pass rate** = (criteria passed) / (criteria applicable).

Criteria are grouped so subtotals are meaningful on their own:

| Group | What it measures | Count |
|---|---|---|
| A. Build & run | Does it compile and start at all | 2 |
| B. API surface | The 14 endpoints exist at the right path/verb with the right success codes | 6 |
| C. Business behavior | The domain rules and state machine are correct | 8 |
| D. Error contract | The spec's error→status mapping is honored | 6 |
| E. Security | Authorization is enforced and failures don't leak or escalate | 6 |
| F. Tests | A test suite exists and passes | 2 |
| **Total** | | **30** |

---

## A. Build & run

| ID | Criterion | Method | Spec |
|---|---|---|---|
| A1 | The solution builds with no errors. | S | — |
| A2 | The service starts and serves `GET /health` → `200`. | P | §7 |

## B. API surface (all require `?api-version=<date>`)

| ID | Criterion | Method | Spec |
|---|---|---|---|
| B1 | `POST /api/customers` returns `201 Created` with a `Location` header on valid input. | P | §6.1, §7 |
| B2 | `POST /api/products` returns `201 Created` with a `Location` header on valid input. | P | §6.2, §7 |
| B3 | `POST /api/orders` returns `201 Created` with a `Location` header on valid input. | P | §6.4, §7 |
| B4 | `GET /api/orders/{id}` returns `200` with the order for an existing id. | P | §6.12, §7 |
| B5 | All 14 documented endpoints exist (no `404`/`405` for a correctly-shaped request to each path/verb). | P | §7 |
| B6 | A request without `?api-version` returns `400`. | P | §7 |

## C. Business behavior

| ID | Criterion | Method | Spec |
|---|---|---|---|
| C1 | Full happy-path lifecycle succeeds: create customer → product → add stock → create order → submit → approve → ship → deliver, each returning the expected status. | P | §4, §6 |
| C2 | Submitting an order reserves stock (product stock decreases by the ordered quantity). | P | §4 |
| C3 | Cancelling a submitted/approved order releases reserved stock (product stock restored). | P | §4 |
| C4 | An invalid transition (e.g. `approve` on a `Draft` order) is rejected (`400`/`422`), not applied. | P | §4 |
| C5 | Submitting with insufficient stock is rejected (`400`/`422`). | P | §4, §6.7 |
| C6 | An order cannot be created/submitted with zero line items; quantity outside 1–999 is rejected. | P | §3.3, §6.4 |
| C7 | Adding the same product twice to one order is rejected or combined (never two line-item rows for one product). | P | §3.3 |
| C8 | Order total equals Σ(unit price × quantity) over line items, using the price captured at add time. (A service that exposes correct line items but no pre-summed aggregate-total field passes on the derived total; see fairness notes.) | P | §3.3 |

## D. Error contract

| ID | Criterion | Method | Spec |
|---|---|---|---|
| D1 | Duplicate customer email → `409 Conflict`. | P | §6.1, §9 |
| D2 | Duplicate product SKU → `409 Conflict`. | P | §6.2, §9 |
| D3 | Operating on a non-existent id → `404 Not Found`. | P | §9 |
| D4 | Invalid input (blank name, bad email) → `422` (the spec's mapping; `400` also accepted). | P | §9 |
| D5 | Error responses are structured (a machine-readable body, e.g. RFC 9457 Problem Details or a consistent JSON error shape) — not an empty body or raw text. | P | §7, §9 |
| D6 | The error→status mapping is **consistent across endpoints** for the categories the spec gives a crisp code: not-found is always `404`, conflict always `409`, forbidden always `403`. (A principled `400` vs `422` split for validation is allowed and not penalized.) | P | §9 |

## E. Security (highest-signal group)

| ID | Criterion | Method | Spec |
|---|---|---|---|
| E1 | A caller missing the required permission gets `403 Forbidden`. | P | §5.3, §9 |
| E2 | Cancel-by-non-owner without admin → `403`; cancel-by-owner → success; cancel-by-admin (other's order) → success. | P | §5.4, §6.11 |
| E3 | A **malformed** `X-Test-Actor` header on a privileged operation does **not** silently elevate to admin — the request is rejected (`400`/`401`/`403`), not executed with full rights. | P | §5.5 |
| E4 | An induced server error in **Production** configuration returns **no stack trace, exception type, or internal path** in the response body. (Tested in Production, not Development, because ASP.NET's developer-exception page leaks by framework default in Development — that is not a code-level vulnerability. If the service cannot boot in Production, E4 is recorded as not-applicable and excluded from that run's denominator.) | P | §9 |
| E5 | Read endpoints requiring `orders:read-all` reject a caller with only `orders:read` → `403`. | P | §5.1, §6.13 |
| E6 | An actor with an **empty permission set** is rejected (`403`) on every privileged operation probed (create / submit / cancel / read-all) — no endpoint is left unguarded. | P | §5.3 |

## F. Tests

| ID | Criterion | Method | Spec |
|---|---|---|---|
| F1 | An automated test project exists and the suite runs. | S | §10 |
| F2 | The test suite passes (0 failures). | S | §10 |

---

## Notes on fairness

- **`X-Test-Actor` absent → default Admin is spec-compliant** (§5.5 mandates it for test
  convenience) and is therefore *not* penalized. E3 tests the distinct, security-relevant
  case of a **malformed** header, where silently elevating to admin is a real vulnerability.
- The probe sends identical requests to every implementation. **For validation failures the spec
  maps to `422`** (RFC 9110 §15.5.21), but the rubric **accepts either `400` or `422`** — both are
  defensible, so neither is penalized, and no framework's default status is rewarded. (This also
  keeps runs generated against the spec's earlier `400` mapping valid; see `METHODOLOGY.md`.)
- **E4 is judged in Production, not Development.** In Development, ASP.NET's developer-exception
  page returns stack traces by framework default for *every* service, so a Development leak is
  not evidence of a code-level fault. E4 boots the service in Production and only fails it if
  internals are leaked there. If the service cannot boot in Production, E4 is not-applicable for
  that run (excluded from the denominator), never a free pass or an unearned fail.
- A criterion that cannot be evaluated because the service failed to start (A2 fails) is
  scored `0` for that run — a service that doesn't run meets no behavioral criteria.
- **C8 accepts a *derived* order total.** The criterion verifies the total *equals*
  Σ(unit price × quantity) at the price captured when each line was added. A service that
  returns correct line items (unit price + quantity) but does not pre-sum an aggregate `total`
  field still **passes**, because the value is present and verifiable; the probe sums the line
  items when no aggregate field is exposed. The missing aggregate-total field is recorded as an
  observation, not a scored failure. This is the credibility-conservative reading — it can only
  *strengthen* an implementation, never manufacture an advantage for one approach over another.
