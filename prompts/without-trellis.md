# Generation Prompt — **Without Trellis (baseline)** arm

> This is the exact prompt handed to each model for the `without-trellis` condition. The only
> difference from [`with-trellis.md`](with-trellis.md) is the framework provided: this arm is
> free to use any approach **except** Trellis. Everything else — the spec, the deliverable,
> the constraints — is identical between the two arms.
>
> The baseline is intentionally **not** hobbled: the model is told to use whatever
> production-grade libraries and patterns it judges best. The goal is a strong, realistic
> from-scratch implementation, so any measured difference reflects the framework, not a
> strawman.

---

You are a senior .NET engineer. Build a complete, working **Order Management** web service in
C# on **.NET 10** that satisfies the attached specification
([`spec/order-management.md`](../spec/order-management.md)) in full.

**Framework for this build:** your choice of standard, widely-used .NET libraries. Start from
`dotnet new web` (or `webapi`) and use whatever production-grade architecture and patterns you
judge best — minimal APIs or controllers, EF Core or another data layer, FluentValidation or
manual validation, etc. Do **not** scaffold from a pre-built domain/service template, and do
**not** build on an opinionated DDD / railway-oriented "result" application framework — assemble
the solution yourself from mainstream libraries. Build it the way a strong .NET team would build
a real service.

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
