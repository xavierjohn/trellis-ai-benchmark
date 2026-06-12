# Raw runs — the inspectable evidence

Each leaf folder is **one complete generated service** — the raw, unedited output of one
model for one condition on one run. Nothing here is hand-fixed; these are exactly what the
model produced from [the spec](../spec/order-management.md) and the matching
[prompt](../prompts/).

```
runs/
├── with-trellis/      <model>/run-{1,2,3}/   ← generated on the Trellis ASP template
└── without-trellis/   <model>/run-{1,2,3}/   ← generated from scratch, no Trellis
```

Models: `gpt-5.5`, `opus-4.8`, `sonnet-4.6`. Three runs each per condition = **18 services**.

## What is and isn't committed

- **Committed:** all source the model wrote — domain, application, persistence, API, tests,
  project/solution files.
- **Not committed** (see `.gitignore`): `bin/`/`obj/` build output, generated `*.db` files,
  and — for `with-trellis` runs — the ~3 MB of identical Trellis API-reference docs the
  template ships in `.github/` (the template version is pinned in each `meta.json`, so the
  starting point is fully reproducible without 9 duplicate copies).

## `meta.json` (one per run)

Every run records an audit trail so the result is traceable to its exact inputs:

```json
{
  "condition": "with-trellis",
  "model": "opus-4.8",
  "run": 1,
  "generated_at": "2026-06-12T23:00:00Z",
  "prompt": "prompts/with-trellis.md",
  "prompt_sha256": "<sha256 of the prompt file at generation time>",
  "spec_sha256": "<sha256 of spec/order-management.md at generation time>",
  "template": "Trellis.AspTemplate 1.0.21-alpha",
  "trellis_packages": "3.0.0-alpha.382",
  "dotnet_sdk": "10.0.301",
  "generator": "manual paste | task-subagent",
  "notes": ""
}
```

For `without-trellis` runs, `template` and `trellis_packages` are `null` and the `notes`
field records the baseline stack the model chose (e.g. "controllers + EF Core + FluentValidation").
