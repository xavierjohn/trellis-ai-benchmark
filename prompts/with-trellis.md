# Generation Prompt — **With Trellis** arm

> This is the exact prompt handed to each model for the `with-trellis` condition. The only
> difference from [`without-trellis.md`](without-trellis.md) is the framework provided: this
> arm starts from the Trellis ASP template. Everything else — the spec, the deliverable, the
> constraints — is identical between the two arms.

---

You are a senior .NET engineer. Build a complete, working **Order Management** web service in
C# on **.NET 10** that satisfies the attached specification
([`spec/order-management.md`](../spec/order-management.md)) in full.

**Framework for this build:** scaffold the solution with the Trellis ASP template:

```
dotnet new install Trellis.AspTemplate
dotnet new trellis-asp -n OrderManagement
```

Implement the service on the Trellis framework, following the guidance and API references
bundled in the generated project's `.github/` folder. Replace the sample `Todo` domain with
the Order Management domain from the spec.

**Deliverable**

- A solution that **builds** with no errors.
- All **14 HTTP endpoints** exactly as listed in spec §7 (paths, verbs, success codes,
  `?api-version` requirement, `Location` headers on creates).
- The full domain behavior in spec §3–§6 (lifecycle state machine, stock reserve/release,
  line-item rules, order total).
- The error contract in spec §9 (validation → 400, not-found → 404, duplicate → 409,
  forbidden → 403) returned as structured error bodies.
- The authorization model in spec §5 (per-operation permissions, the cancel-ownership check,
  the `X-Test-Actor` header convention).
- An **automated test suite** that runs and passes (spec §10).
- SQLite persistence as described in spec §8.

**Rules**

- Build it to completion. Do not ask clarifying questions; make reasonable engineering
  decisions where the spec is silent.
- Use `TimeProvider` for time-dependent logic (no `DateTime.UtcNow` in domain code).
- Do not hard-code test-only shortcuts into production code paths.
- When you are done, the service must start and serve `GET /health` → 200.
