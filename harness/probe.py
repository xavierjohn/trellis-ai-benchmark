"""Framework-neutral black-box probe for the Order Management benchmark.

Boots-agnostic: this module talks to an already-running service over HTTP only. It never
inspects source code, so it scores the *same* observable behavior for every implementation
regardless of framework, language idiom, or architecture.

Each method evaluates one rubric criterion (see ../rubric/neutral-rubric.md) and records a
1 (pass) / 0 (fail) plus human-readable evidence. The public entry point is `run_probe`.
"""

from __future__ import annotations

import argparse
import json
import re
import uuid
from dataclasses import dataclass, field
from typing import Any

import requests

# High-confidence markers that an error body leaked .NET internals (rubric E4). Kept narrow
# to avoid false positives on legitimate Problem Details bodies.
LEAK_PATTERNS = [
    re.compile(r"\n\s+at [\w.`<>+\[\]]+\("),   # stack frame: "   at Namespace.Method("
    re.compile(r"\.cs:line \d+"),               # source location
    re.compile(r"End of stack trace"),
    re.compile(r"System\.[A-Za-z0-9_.]*Exception"),  # leaked exception type
]

# api-version candidates tried in order. The spec example is 2026-11-12; the rest cover
# common date-version choices so the probe can talk to a faithful service that picked a
# different date. The chosen value is recorded in the result; the *specific* date is not
# scored (only "a date version is required", rubric B6).
VERSION_CANDIDATES = ["2026-11-12", "2026-12-01", "2026-03-26", "2026-01-01"]

VALIDATION_OK = {400, 422}          # spec §9 allows either for validation
HANDLED = {200, 201, 400, 401, 403, 409, 422}  # "the route exists and was handled"

# The spec (§6.4) says order creation takes a "list of (productId, quantity)" but does NOT pin
# the JSON field name for that list. Implementations reasonably differ (lineItems / Lines /
# items / ...). To stay fair, the probe sends the list under every common alias; the service
# binds whichever it expects and (with default System.Text.Json) ignores the rest.
LINE_FIELD_ALIASES = ("lineItems", "lines", "items", "orderLines", "lineItemRequests", "orderLineItems")


def find_leaks(bodies) -> list[str]:
    """Return the distinct leak-marker patterns found across response bodies (rubric E4)."""
    found = set()
    for body in bodies or []:
        for pat in LEAK_PATTERNS:
            if pat.search(body or ""):
                found.add(pat.pattern)
    return sorted(found)


@dataclass
class Probe:
    base_url: str
    api_version: str | None = None
    results: dict[str, dict[str, Any]] = field(default_factory=dict)
    _error_bodies: list[str] = field(default_factory=list)
    _notfound_statuses: set[int] = field(default_factory=set)
    _conflict_statuses: set[int] = field(default_factory=set)
    _forbidden_statuses: set[int] = field(default_factory=set)

    # ---- low-level HTTP -------------------------------------------------
    def _url(self, path: str, version: bool = True) -> str:
        url = self.base_url.rstrip("/") + path
        if version and self.api_version:
            sep = "&" if "?" in url else "?"
            url = f"{url}{sep}api-version={self.api_version}"
        return url

    def _actor(self, actor: Any) -> dict[str, str]:
        """Build the X-Test-Actor header. `actor` may be None (omit → default Admin),
        a dict (serialized to JSON), or a raw string (sent verbatim, e.g. malformed)."""
        if actor is None:
            return {}
        if isinstance(actor, str):
            return {"X-Test-Actor": actor}
        return {"X-Test-Actor": json.dumps(actor)}

    def req(self, method: str, path: str, *, actor: Any = None, json_body: Any = None,
            version: bool = True) -> requests.Response:
        headers = self._actor(actor)
        if json_body is not None:
            headers["Content-Type"] = "application/json"
        resp = requests.request(
            method, self._url(path, version), headers=headers,
            data=json.dumps(json_body) if json_body is not None else None,
            timeout=30,
        )
        # Capture every error body once, for the global leak scan (E4) and consistency (D6).
        if resp.status_code >= 400:
            self._error_bodies.append(resp.text or "")
        return resp

    # ---- helpers --------------------------------------------------------
    @staticmethod
    def _json(resp: requests.Response) -> Any:
        try:
            return resp.json()
        except Exception:
            return None

    @staticmethod
    def _find_id(resp: requests.Response) -> str | None:
        loc = resp.headers.get("Location")
        if loc:
            seg = loc.rstrip("/").split("/")[-1].split("?")[0]
            if seg:
                return seg
        body = Probe._json(resp)
        if isinstance(body, dict):
            for k in ("id", "customerId", "productId", "orderId", "orderID"):
                v = body.get(k)
                if isinstance(v, str) and v:
                    return v
        return None

    @staticmethod
    def _walk(obj: Any):
        yield obj
        if isinstance(obj, dict):
            for v in obj.values():
                yield from Probe._walk(v)
        elif isinstance(obj, list):
            for v in obj:
                yield from Probe._walk(v)

    @staticmethod
    def _find_num(obj: Any, key_substrings: tuple[str, ...]) -> float | None:
        for node in Probe._walk(obj):
            if isinstance(node, dict):
                for k, v in node.items():
                    if any(s in k.lower() for s in key_substrings) and isinstance(v, (int, float)):
                        return float(v)
        return None

    @staticmethod
    def _line_items(order: Any) -> list:
        for node in Probe._walk(order):
            if isinstance(node, dict):
                for k, v in node.items():
                    if ("lineitem" in k.lower() or k.lower() in ("items", "lines")) and isinstance(v, list):
                        return v
        return []

    @staticmethod
    def _status_of(order: Any) -> str | None:
        if isinstance(order, dict):
            for k, v in order.items():
                if k.lower() in ("status", "orderstatus") and isinstance(v, str):
                    return v
        return None

    def record(self, cid: str, ok: bool, evidence: str) -> bool:
        self.results[cid] = {"pass": 1 if ok else 0, "evidence": evidence}
        return ok

    # ---- payload factories ---------------------------------------------
    @staticmethod
    def _customer(email: str | None = None, **over) -> dict:
        body = {
            "firstName": "Ada", "lastName": "Lovelace",
            "email": email or f"ada-{uuid.uuid4().hex[:10]}@example.com",
            "phoneNumber": "+1-202-555-0142",
            "shippingAddress": {
                "street": "1 Analytical Way", "city": "London", "state": "LDN",
                "postalCode": "EC1A1BB", "country": "GB",
            },
        }
        body.update(over)
        return body

    @staticmethod
    def _product(sku: str | None = None, price: float = 10.0, **over) -> dict:
        body = {
            "productName": "Widget",
            "sku": sku or "SKU" + uuid.uuid4().hex[:10].upper(),
            "unitPrice": price,
        }
        body.update(over)
        return body

    def _make_customer(self, actor: Any = None) -> str | None:
        r = self.req("POST", "/api/customers", actor=actor, json_body=self._customer())
        return self._find_id(r) if r.status_code == 201 else None

    def _make_product(self, price: float = 10.0, stock: int = 0, actor: Any = None) -> str | None:
        r = self.req("POST", "/api/products", actor=actor, json_body=self._product(price=price))
        pid = self._find_id(r) if r.status_code == 201 else None
        if pid and stock:
            self.req("POST", f"/api/products/{pid}/stock-additions", actor=actor,
                     json_body={"quantity": stock})
        return pid

    def _make_order(self, customer_id: str, product_id: str, qty: int = 1, actor: Any = None) -> str | None:
        body = self._order_body(customer_id, [{"productId": product_id, "quantity": qty}])
        r = self.req("POST", "/api/orders", actor=actor, json_body=body)
        return self._find_id(r) if r.status_code == 201 else None

    @staticmethod
    def _order_body(customer_id, lines):
        """Order-create payload with the line list sent under every common field-name alias
        (see LINE_FIELD_ALIASES), so a service is not failed for a reasonable naming choice."""
        body = {"customerId": customer_id}
        for key in LINE_FIELD_ALIASES:
            body[key] = lines
        return body

    # ---- version detection ---------------------------------------------
    def detect_version(self) -> None:
        for v in VERSION_CANDIDATES:
            self.api_version = v
            r = self.req("GET", "/api/orders/overdue")
            if r.status_code == 200:
                return
        self.api_version = VERSION_CANDIDATES[0]  # fall back to spec example

    # ====================================================================
    #  CRITERIA
    # ====================================================================
    def check_A2_health(self):
        r = requests.get(self._url("/health", version=False), timeout=15)
        self.record("A2", r.status_code == 200, f"GET /health -> {r.status_code}")

    def check_B_surface_and_C_D_E(self):
        v = self.api_version
        # --- B1 / D1 / D4 : customers ---
        email = f"dup-{uuid.uuid4().hex[:8]}@example.com"
        r = self.req("POST", "/api/customers", json_body=self._customer(email=email))
        cust = self._find_id(r)
        self.record("B1", r.status_code == 201 and bool(r.headers.get("Location")),
                    f"POST /api/customers -> {r.status_code}, Location={r.headers.get('Location')!r}")
        rdup = self.req("POST", "/api/customers", json_body=self._customer(email=email))
        self.record("D1", rdup.status_code == 409, f"duplicate email -> {rdup.status_code}")
        self._conflict_statuses.add(rdup.status_code)
        rbad = self.req("POST", "/api/customers", json_body=self._customer(firstName="", email="not-an-email"))
        self.record("D4", rbad.status_code in VALIDATION_OK, f"invalid customer -> {rbad.status_code}")

        # --- B2 / D2 : products ---
        sku = "SKU" + uuid.uuid4().hex[:8].upper()
        rp = self.req("POST", "/api/products", json_body=self._product(sku=sku, price=10.0))
        prod = self._find_id(rp)
        self.record("B2", rp.status_code == 201 and bool(rp.headers.get("Location")),
                    f"POST /api/products -> {rp.status_code}, Location={rp.headers.get('Location')!r}")
        rpdup = self.req("POST", "/api/products", json_body=self._product(sku=sku, price=10.0))
        self.record("D2", rpdup.status_code == 409, f"duplicate SKU -> {rpdup.status_code}")
        self._conflict_statuses.add(rpdup.status_code)

        # --- B3 : order create (price 10 x qty 2 -> total 20) ---
        order = None
        if cust and prod:
            self.req("POST", f"/api/products/{prod}/stock-additions", json_body={"quantity": 100})
            ro = self.req("POST", "/api/orders",
                          json_body=self._order_body(cust, [{"productId": prod, "quantity": 2}]))
            order = self._find_id(ro)
            self.record("B3", ro.status_code == 201 and bool(ro.headers.get("Location")),
                        f"POST /api/orders -> {ro.status_code}, Location={ro.headers.get('Location')!r}")
        else:
            self.record("B3", False, "skipped: customer or product creation failed")

        # --- B4 / C8 / C7 : read order, total, duplicate-line-item ---
        if order:
            rget = self.req("GET", f"/api/orders/{order}")
            body = self._json(rget)
            self.record("B4", rget.status_code == 200 and body is not None,
                        f"GET /api/orders/{{id}} -> {rget.status_code}")
            total = self._find_num(body, ("total", "amount"))
            self.record("C8", total is not None and abs(total - 20.0) < 0.01,
                        f"order total={total} (expected 20.0 = 10.00 x 2)")
            # add the SAME product again -> must be rejected or combined (never 2 rows)
            radd = self.req("POST", f"/api/orders/{order}/line-items",
                            json_body={"productId": prod, "quantity": 1})
            if radd.status_code in VALIDATION_OK:
                self.record("C7", True, f"duplicate line-item rejected -> {radd.status_code}")
            else:
                rget2 = self.req("GET", f"/api/orders/{order}")
                items = self._line_items(self._json(rget2))
                matches = sum(1 for it in items if prod and prod in json.dumps(it))
                self.record("C7", matches == 1,
                            f"duplicate line-item accepted ({radd.status_code}); rows for product={matches}")
        else:
            for c in ("B4", "C8", "C7"):
                self.record(c, False, "skipped: order creation failed")

        # --- C6 : empty order / out-of-range quantity rejected ---
        if cust and prod:
            r_empty = self.req("POST", "/api/orders", json_body=self._order_body(cust, []))
            r_zero = self.req("POST", "/api/orders",
                              json_body=self._order_body(cust, [{"productId": prod, "quantity": 0}]))
            r_big = self.req("POST", "/api/orders",
                             json_body=self._order_body(cust, [{"productId": prod, "quantity": 1000}]))
            ok = all(x.status_code in VALIDATION_OK for x in (r_empty, r_zero, r_big))
            self.record("C6", ok,
                        f"empty={r_empty.status_code} qty0={r_zero.status_code} qty1000={r_big.status_code}")
        else:
            self.record("C6", False, "skipped: prerequisites failed")

        # --- C4 : invalid transition (approve a Draft) ---
        if order:
            rinv = self.req("POST", f"/api/orders/{order}/approval")
            self.record("C4", rinv.status_code in VALIDATION_OK,
                        f"approve(Draft) -> {rinv.status_code}")
        else:
            self.record("C4", False, "skipped: order creation failed")

        # --- D3 : not found ---
        missing = str(uuid.uuid4())
        rnf = self.req("GET", f"/api/orders/{missing}")
        self.record("D3", rnf.status_code == 404, f"GET missing order -> {rnf.status_code}")
        if rnf.status_code == 404:
            self._notfound_statuses.add(404)
        rnf2 = self.req("POST", f"/api/products/{missing}/stock-additions", json_body={"quantity": 5})
        self._notfound_statuses.add(rnf2.status_code)

        # --- D5 : structured error body ---
        self.record("D5", self._is_structured(rnf) and self._is_structured(rbad),
                    f"404 ct={rnf.headers.get('Content-Type')!r}; 400 ct={rbad.headers.get('Content-Type')!r}")

        # (D6 is finalized after the security probes — see check_D6_consistency.)

        # --- B5 : all 14 endpoints exist (real ids; never 404-route / 405) ---
        self._check_b5(cust, prod, order)

        # --- B6 : missing api-version -> 400 ---
        rnv = self.req("GET", "/api/orders/overdue", version=False)
        self.record("B6", rnv.status_code == 400, f"no api-version -> {rnv.status_code}")

    def _is_structured(self, resp: requests.Response) -> bool:
        if resp.status_code < 400:
            return False
        ct = (resp.headers.get("Content-Type") or "").lower()
        body = self._json(resp)
        if isinstance(body, dict) and any(
            k in body for k in ("type", "title", "status", "detail", "errors", "code", "message")
        ):
            return True
        return "problem+json" in ct and bool(resp.text.strip())

    def _check_b5(self, cust, prod, order):
        if not (cust and prod and order):
            return self.record("B5", False, "skipped: customer/product/order setup failed")
        rand = str(uuid.uuid4())
        # (method, path, body, real_id) — real_id=True means a 404 indicates a MISSING ROUTE
        # (we used a known-existing id); real_id=False means a 404 is a legitimate
        # entity-not-found for a synthetic sub-id and still proves the route exists.
        calls = [
            ("POST", "/api/customers", self._customer(), True),
            ("POST", "/api/products", self._product(), True),
            ("POST", f"/api/products/{prod}/stock-additions", {"quantity": 1}, True),
            ("POST", "/api/orders", self._order_body(cust, [{"productId": prod, "quantity": 1}]), True),
            ("POST", f"/api/orders/{order}/line-items", {"productId": rand, "quantity": 1}, False),
            ("DELETE", f"/api/orders/{order}/line-items/{rand}", None, False),
            ("POST", f"/api/orders/{order}/submission", None, True),
            ("POST", f"/api/orders/{order}/approval", None, True),
            ("POST", f"/api/orders/{order}/shipment", None, True),
            ("POST", f"/api/orders/{order}/delivery", None, True),
            ("POST", f"/api/orders/{order}/cancellation", None, True),
            ("GET", f"/api/orders/{order}", None, True),
            ("GET", f"/api/customers/{cust}/orders", None, True),
            ("GET", "/api/orders/overdue", None, True),
        ]
        missing = []
        for method, path, body, real_id in calls:
            r = self.req(method, path, json_body=body)
            if r.status_code == 405:
                missing.append(f"{method} {path} -> 405")
            elif r.status_code == 404 and real_id:
                missing.append(f"{method} {path} -> 404 (route missing)")
        self.record("B5", not missing,
                    "all 14 routes present" if not missing else f"missing/misrouted: {missing}")

    def check_C1_lifecycle(self):
        cust = self._make_customer()
        prod = self._make_product(price=5.0, stock=50)
        if not (cust and prod):
            return self.record("C1", False, "setup failed")
        order = self._make_order(cust, prod, qty=3)
        if not order:
            return self.record("C1", False, "order create failed")
        steps = [
            ("submission", 200), ("approval", 200), ("shipment", 200), ("delivery", 200),
        ]
        trail = []
        for sub, expect in steps:
            r = self.req("POST", f"/api/orders/{order}/{sub}")
            trail.append(f"{sub}->{r.status_code}")
            if r.status_code != expect:
                return self.record("C1", False, "; ".join(trail))
        final = self.req("GET", f"/api/orders/{order}")
        status = self._status_of(self._json(final)) or ""
        ok = status.lower() == "delivered"
        self.record("C1", ok, "; ".join(trail) + f"; final status={status!r}")

    def check_C2_C3_C5_stock(self):
        """Reserve (C2), insufficient (C5) and release (C3) — proven behaviorally because the
        API exposes no product-read endpoint. Stock=5: order A qty5 submit consumes it;
        order B qty1 submit then fails; cancel A releases; B submit then succeeds."""
        cust = self._make_customer()
        prod = self._make_product(price=5.0, stock=5)
        if not (cust and prod):
            for c in ("C2", "C3", "C5"):
                self.record(c, False, "setup failed")
            return
        a = self._make_order(cust, prod, qty=5)
        b = self._make_order(cust, prod, qty=1)
        if not (a and b):
            for c in ("C2", "C3", "C5"):
                self.record(c, False, "order create failed")
            return
        ra = self.req("POST", f"/api/orders/{a}/submission")
        rb1 = self.req("POST", f"/api/orders/{b}/submission")   # should fail: stock exhausted
        rc = self.req("POST", f"/api/orders/{a}/cancellation")  # release 5
        rb2 = self.req("POST", f"/api/orders/{b}/submission")   # should now succeed

        self.record("C2", ra.status_code == 200 and rb1.status_code in VALIDATION_OK,
                    f"submitA(qty5)->{ra.status_code}; submitB(qty1 after)->{rb1.status_code} (reserve consumed stock)")
        self.record("C5", rb1.status_code in VALIDATION_OK,
                    f"submit with insufficient stock -> {rb1.status_code}")
        self.record("C3", rc.status_code == 200 and rb2.status_code == 200,
                    f"cancelA->{rc.status_code}; submitB after release->{rb2.status_code}")

    def check_E_security(self):
        admin = None  # absent header -> default admin (spec §5.5)
        # --- E1 : missing required permission -> 403 ---
        weak = {"id": "weak-1", "permissions": ["orders:read"]}
        r = self.req("POST", "/api/customers", actor=weak, json_body=self._customer())
        self.record("E1", r.status_code == 403, f"create customer w/o customers:create -> {r.status_code}")
        self._forbidden_statuses.add(r.status_code)

        # --- E5 : read-all required, only read -> 403 ---
        reader = {"id": "reader-1", "permissions": ["orders:read"]}
        r5 = self.req("GET", "/api/orders/overdue", actor=reader)
        cust = self._make_customer(actor=admin)
        r5b = self.req("GET", f"/api/customers/{cust}/orders", actor=reader) if cust else None
        ok5 = r5.status_code == 403 and (r5b is None or r5b.status_code == 403)
        self.record("E5", ok5,
                    f"overdue w/ read-only -> {r5.status_code}; list-by-customer -> {getattr(r5b,'status_code',None)}")
        self._forbidden_statuses.add(r5.status_code)
        if r5b is not None:
            self._forbidden_statuses.add(r5b.status_code)

        # --- E2 : cancel ownership ---
        prod = self._make_product(price=5.0, stock=20, actor=admin)
        alice = {"id": "alice", "permissions": ["orders:create", "orders:cancel", "orders:read"]}
        bob = {"id": "bob", "permissions": ["orders:create", "orders:cancel", "orders:read"]}
        o_alice = self._make_order(cust, prod, qty=1, actor=alice) if (cust and prod) else None
        if o_alice:
            r_bob = self.req("POST", f"/api/orders/{o_alice}/cancellation", actor=bob)      # non-owner
            r_admin = self.req("POST", f"/api/orders/{o_alice}/cancellation", actor=admin)  # admin overrides
            o2 = self._make_order(cust, prod, qty=1, actor=alice)
            r_owner = self.req("POST", f"/api/orders/{o2}/cancellation", actor=alice) if o2 else None
            ok2 = (r_bob.status_code == 403 and r_admin.status_code == 200
                   and r_owner is not None and r_owner.status_code == 200)
            self.record("E2", ok2,
                        f"non-owner->{r_bob.status_code}; admin->{r_admin.status_code}; owner->{getattr(r_owner,'status_code',None)}")
            self._forbidden_statuses.add(r_bob.status_code)
        else:
            self.record("E2", False, "setup failed (no order)")

        # --- E3 : malformed actor header must NOT silently elevate to admin ---
        for malformed in ('not-json-{{{', '{"id":"x"', '{"permissions":"orders:create"}'):
            rm = self.req("POST", "/api/customers", actor=malformed, json_body=self._customer())
            if rm.status_code == 201:
                self.record("E3", False, f"malformed actor {malformed!r} created a customer (201) — silent elevation")
                break
        else:
            self.record("E3", True, "malformed actor header rejected on all variants (no silent elevation)")

        # --- E6 : empty-permission actor rejected on every privileged op ---
        empty = {"id": "nobody", "permissions": []}
        probes = [
            ("POST", "/api/customers", self._customer()),
            ("POST", f"/api/orders/{o_alice or uuid.uuid4()}/submission", None),
            ("POST", f"/api/orders/{o_alice or uuid.uuid4()}/cancellation", None),
            ("GET", "/api/orders/overdue", None),
        ]
        bad = []
        for m, p, b in probes:
            rr = self.req(m, p, actor=empty, json_body=b)
            if rr.status_code not in (403, 401):
                if not (rr.status_code == 404):  # 404 from a missing id still means authz didn't pass-through to success
                    bad.append(f"{m} {p}->{rr.status_code}")
        self.record("E6", not bad, "all privileged ops denied for empty-perm actor" if not bad else f"unguarded: {bad}")

    def check_D6_consistency(self):
        """The spec gives crisp codes to whole categories: not-found=404, conflict=409,
        forbidden=403. D6 verifies the mapping is *consistent across endpoints* for those
        categories. It deliberately does NOT demand a single validation status, because a
        principled 400 (syntactic) vs 422 (semantic) split is a legitimate, consistent design."""
        nf = self._notfound_statuses
        cf = self._conflict_statuses
        fb = self._forbidden_statuses
        nf_ok = bool(nf) and nf <= {404}
        cf_ok = bool(cf) and cf <= {409}
        fb_ok = bool(fb) and fb <= {403}
        self.record("D6", nf_ok and cf_ok and fb_ok,
                    f"not-found={sorted(nf)} (want all 404); conflict={sorted(cf)} (want all 409); "
                    f"forbidden={sorted(fb)} (want all 403)")

    def check_E4_no_leak(self):
        """Deprecated for scoring: in Development, ASP.NET's developer exception page leaks
        by framework default, which is not a real production vulnerability. E4 is scored by
        the harness against a PRODUCTION boot instead (run_all.production_leak_check). This
        method is retained only as a development-mode diagnostic and is not called by run()."""
        leaked = find_leaks(self._error_bodies)
        self.record("E4", not leaked,
                    "no stack/exception leakage in any error body"
                    if not leaked else f"leak markers found: {leaked}")

    # ---- driver ---------------------------------------------------------
    def run(self) -> dict:
        self._safe(self.check_A2_health)
        try:
            self.detect_version()
        except Exception as e:  # noqa: BLE001
            self.api_version = VERSION_CANDIDATES[0]
            self.results.setdefault("_version_error", {"pass": 0, "evidence": repr(e)})
        self._safe(self.check_B_surface_and_C_D_E)
        self._safe(self.check_C1_lifecycle)
        self._safe(self.check_C2_C3_C5_stock)
        self._safe(self.check_E_security)
        self._safe(self.check_D6_consistency)  # after E: needs forbidden statuses
        # E4 (no internal leak) is NOT scored here — the harness scores it against a
        # PRODUCTION boot (run_all.production_leak_check), because the dev-mode developer
        # exception page leaks by framework default and is not a production vulnerability.
        return {
            "api_version": self.api_version,
            "criteria": {k: v for k, v in self.results.items() if not k.startswith("_")},
        }

    def _safe(self, fn):
        try:
            fn()
        except Exception as e:  # noqa: BLE001
            self.results.setdefault(f"_error_{fn.__name__}", {"pass": 0, "evidence": repr(e)})


def run_probe(base_url: str, api_version: str | None = None) -> dict:
    return Probe(base_url=base_url, api_version=api_version).run()


def main():
    ap = argparse.ArgumentParser(description="Black-box probe against a running service.")
    ap.add_argument("base_url", help="e.g. http://127.0.0.1:5005")
    ap.add_argument("--api-version", default=None)
    args = ap.parse_args()
    out = run_probe(args.base_url, args.api_version)
    print(json.dumps(out, indent=2))


if __name__ == "__main__":
    main()
