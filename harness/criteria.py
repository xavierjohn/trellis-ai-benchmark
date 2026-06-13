"""Canonical list of rubric criteria (keep in sync with ../rubric/neutral-rubric.md)."""

# id -> (group, method, short label). P = black-box probe, S = static check.
CRITERIA = {
    "A1": ("A", "S", "Builds with no errors"),
    "A2": ("A", "P", "Service starts; GET /health -> 200"),
    "B1": ("B", "P", "POST /customers -> 201 + Location"),
    "B2": ("B", "P", "POST /products -> 201 + Location"),
    "B3": ("B", "P", "POST /orders -> 201 + Location"),
    "B4": ("B", "P", "GET /orders/{id} -> 200"),
    "B5": ("B", "P", "All 14 endpoints exist"),
    "B6": ("B", "P", "Missing api-version -> 400"),
    "C1": ("C", "P", "Full lifecycle succeeds"),
    "C2": ("C", "P", "Submit reserves stock"),
    "C3": ("C", "P", "Cancel releases stock"),
    "C4": ("C", "P", "Invalid transition rejected"),
    "C5": ("C", "P", "Insufficient stock rejected"),
    "C6": ("C", "P", "Empty/out-of-range qty rejected"),
    "C7": ("C", "P", "Duplicate product line rejected/combined"),
    "C8": ("C", "P", "Order total correct"),
    "D1": ("D", "P", "Duplicate email -> 409"),
    "D2": ("D", "P", "Duplicate SKU -> 409"),
    "D3": ("D", "P", "Not found -> 404"),
    "D4": ("D", "P", "Invalid input -> 400/422"),
    "D5": ("D", "P", "Structured error body"),
    "D6": ("D", "P", "Status mapping consistent"),
    "E1": ("E", "P", "Missing permission -> 403"),
    "E2": ("E", "P", "Cancel ownership enforced"),
    "E3": ("E", "P", "Malformed actor not elevated"),
    "E4": ("E", "P", "No stack/exception leak (in Production)"),
    "E5": ("E", "P", "read-all enforced vs read-only"),
    "E6": ("E", "P", "Empty-perm actor fully denied"),
    "F1": ("F", "S", "Test suite exists and runs"),
    "F2": ("F", "S", "Test suite passes"),
}

GROUPS = {
    "A": "Build & run",
    "B": "API surface",
    "C": "Business behavior",
    "D": "Error contract",
    "E": "Security",
    "F": "Tests",
}

ALL_IDS = list(CRITERIA.keys())
