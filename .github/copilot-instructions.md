# Copilot Instructions — seclai-csharp

## Build & Lint Pipeline

```sh
dotnet build Seclai.sln    # build
dotnet test Seclai.sln     # tests (xUnit)
```

## Quality gates (must pass to report completion)

- **ALL tests must pass with ZERO failures. No exceptions.** CI/CD runs the full test suite on every PR. A test failure blocks the build.
- **`dotnet build Seclai.sln` must succeed with ZERO errors and ZERO warnings treated as errors.**
- **Do not dismiss test or build failures as pre-existing or unrelated.** The `main` branch CI/CD is green. Any failure on a feature branch was caused by changes on that branch.
- **CRITICAL — NEVER INVESTIGATE ERROR ORIGIN OR BLAME**: When a build or test error appears, **fix it immediately**. Do NOT run `git blame` or use git history to argue that an error is "pre-existing" or not your responsibility. Tools like `git diff`, `git log`, and `git show` may be used to understand and review changes, but never to avoid fixing an error. There is no scenario where knowing the origin of an error changes what you must do: **fix it**.
- **CRITICAL — NEVER PIPE TEST OR BUILD OUTPUT**: Do not append `| tail`, `| head`, `| grep`, or any pipe to `dotnet build`, `dotnet test`, or similar commands. Piping hides errors. Always run with full unfiltered output.

## Key Rules

- `SeclaiClient` requires `SeclaiClientOptions` — there is no parameterless constructor. README/doc examples must use `new SeclaiClient(new SeclaiClientOptions { ... })`.
- Tests that mutate process-wide environment variables (`SECLAI_API_KEY`, `SECLAI_CONFIG_DIR`) must save original values and restore them in `finally` for both variables. Use `Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())` instead of hard-coded Unix paths.
- `WriteSsoCache` uses `File.Replace()` when the destination exists (cross-platform atomic overwrite), with `File.Move()` as fallback for first write. Do not claim atomicity in doc comments.
- `ApplyAuthAsync` passes `null` for the httpClient parameter so SSO token refresh uses a clean internal `HttpClient` — do not pass `_http` or caller default headers will leak to the Cognito endpoint.
- `AuthState` does not have an `HttpClient` property — it was removed as dead state.
- Avoid `catch (Exception ex) when (ex is X or Y)` if `ex` is unused — use separate `catch (X)` / `catch (Y)` blocks.
- Target framework: `netstandard2.0` (library), `net10.0` (tests). No OpenAPI spec file in this repo.
- `.github/copilot-instructions.md` shares common sections (quality gates, git rules, editing rules, self-correction rules) across all SDK repos. When updating shared rules, apply the same change to all repos: `seclai-python`, `seclai-javascript`, `seclai-go`, `seclai-csharp`, `seclai-cli`, `seclai-mcp`.
- Do not run ad-hoc scripts; add tests instead.

## Git rules

- **NEVER use `git stash`.** Use `git diff`, `git log`, or `git show` instead.
- Do not run `git checkout` to switch branches, `git reset`, or any other destructive git operation without explicit user approval.

## Editing rules

- Do not use CLI text tools (sed/awk). Use the editor-based patch tool.

## Self-correction rules

- **NEVER promise to "do better" without updating these instruction files.** If a recurring mistake is identified, edit this file with a concrete rule that prevents the mistake. Do that FIRST, then continue work.
