# Copilot Instructions — seclai-csharp

## Build & Lint Pipeline

```sh
dotnet build Seclai.sln    # build
dotnet test Seclai.sln     # tests (xUnit)
```

## Key Rules

- `SeclaiClient` requires `SeclaiClientOptions` — there is no parameterless constructor. README/doc examples must use `new SeclaiClient(new SeclaiClientOptions { ... })`.
- Tests that mutate process-wide environment variables (`SECLAI_API_KEY`, `SECLAI_CONFIG_DIR`) must save original values and restore them in `finally` for both variables. Use `Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())` instead of hard-coded Unix paths.
- `WriteSsoCache` uses `File.Replace()` when the destination exists (cross-platform atomic overwrite), with `File.Move()` as fallback for first write. Do not claim atomicity in doc comments.
- `ApplyAuthAsync` passes `null` for the httpClient parameter so SSO token refresh uses a clean internal `HttpClient` — do not pass `_http` or caller default headers will leak to the Cognito endpoint.
- `AuthState` does not have an `HttpClient` property — it was removed as dead state.
- Avoid `catch (Exception ex) when (ex is X or Y)` if `ex` is unused — use separate `catch (X)` / `catch (Y)` blocks.
- Target framework: `netstandard2.0` (library), `net10.0` (tests). No OpenAPI spec file in this repo.
