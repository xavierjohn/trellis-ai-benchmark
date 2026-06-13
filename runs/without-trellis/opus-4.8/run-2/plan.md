# Order Management System — Build Plan

## Architecture
Layered solution, .NET 10:
- `src/OrderManagement.Domain` — aggregates, value objects, state machine, Result/Error, domain events.
- `src/OrderManagement.Application` — use-case handlers, repository interfaces, IActor/authorization, DTOs.
- `src/OrderManagement.Infrastructure` — EF Core DbContext (SQLite), repositories.
- `src/OrderManagement.Api` — minimal API endpoints, api-versioning, actor provider (X-Test-Actor), ProblemDetails mapping.
- `tests/OrderManagement.Domain.Tests`
- `tests/OrderManagement.Application.Tests`
- `tests/OrderManagement.Api.Tests`

## Key decisions
- Result<T> with Error kinds: Validation(400), NotFound(404), Conflict(409), Forbidden(403).
- TimeProvider injected everywhere time is needed.
- Asp.Versioning, query param `api-version`, required (missing → 400).
- EF Core SQLite, EnsureCreated. Integration tests use in-memory SQLite connection.
- xUnit + FluentAssertions + Moq.

## Phases
1. Scaffold solution + projects + packages. [done-ish]
2. Domain layer + domain tests.
3. Application layer + application tests.
4. Infrastructure (EF Core).
5. Api layer (endpoints, versioning, actor, problem details).
6. Integration tests.
7. Build, run all tests, verify /health.
