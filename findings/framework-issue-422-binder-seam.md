# Framework issue draft — value-object DTO validation returns hardcoded 422

> Ready to file against **`xavierjohn/Trellis`**. Surfaced by this benchmark (4/4 with-Trellis
> builds flagged it). Source line numbers verified against `main` on 2026-06-12.

---

**Title:** `ASP: value-object request-DTO validation returns a hardcoded 422 that MapError can't override`

**Labels:** `area: asp`, `enhancement`, `dx`

---

## Summary

When request DTOs use value-object (`IScalarValue`) properties — the framework's recommended
pattern — a semantic validation failure during JSON deserialization returns a **hardcoded HTTP
422** that `MapError<Error.InvalidInput>(…)` does **not** override. A service whose spec requires
a different validation status (commonly `400`) cannot get it uniformly without abandoning
value-object request DTOs for primitive DTOs + manual `TryCreate`/`Combine` in the controller —
which contradicts the framework's own "value objects everywhere" guidance.

## Two validation→status paths (one configurable, one not)

1. **Handler / `Result` errors** — `Error.InvalidInput` defaults to `422` but is **overridable**:
   - `Trellis.Asp/src/TrellisAspOptions.cs:137-138` (default map `InvalidInput`/`InvariantViolation` → 422)
   - `Trellis.Asp/src/TrellisAspOptions.cs:158` (`MapError<Error.InvalidInput>(status)`)

2. **Value-object request-DTO binding** — **hardcoded `422`, bypassing the map**:
   - `Trellis.Asp/src/Validation/ScalarValueValidationMiddleware.cs:184,189` set
     `StatusCodes.Status422UnprocessableEntity` for semantic-validation failures;
     `:201-204` only drops to `400` when the bytes are not valid JSON. The configured error map
     is never consulted.
   - Failures are surfaced via `ValidationErrorsContext` by the generated converters
     (`ScalarValueJsonConverterGenerator`, `Validation/PrimitiveJsonReader.cs`,
     `Validation/ScalarValueJsonConverterBase.cs`), which the middleware turns into `422`.

## Impact

- A spec that wants `400` for validation forces **primitive DTOs + manual VO reconstruction**,
  contradicting the recommended value-object-in-DTO pattern.
- **Inconsistency:** handler-level validation can be remapped (e.g. to `400`) while binder-level
  validation stays `422` — the same logical error class yielding two different status codes in one
  service.
- This was the **single most-cited friction** across an AI-generation benchmark: 4/4 with-Trellis
  builds (GPT-5.5 ×2, opus-4.8 ×2) independently flagged it; one reported it "cost the most
  investigation time" and that it "isn't documented as a tradeoff."

## Proposed resolution (either)

1. **Preferred:** make `ScalarValueValidationMiddleware` consult the configured error map (the same
   `MapError<Error.InvalidInput>` mapping used for handler errors), so binder-level semantic
   validation honors the same status as handler-level `Error.InvalidInput`. This makes the two
   paths consistent and configurable.
2. **Or:** document the primitive-DTO + `TryCreate`/`Combine` pattern as the supported way to get
   non-`422` validation semantics, explicitly calling out the value-object-DTO `422` tradeoff.

## Note (separate, lower priority)

While verifying the above, the benchmark also found that the template's
`AddDevelopmentActorProvider(DefaultPermissions = Permissions.All)` resolves a **malformed**
`X-Test-Actor` header to the default admin actor (silent elevation) in Development. It's a
dev-only test affordance and guarded from production, but rejecting a malformed header rather than
falling back to the default would be safer. Worth a separate issue if you agree.
