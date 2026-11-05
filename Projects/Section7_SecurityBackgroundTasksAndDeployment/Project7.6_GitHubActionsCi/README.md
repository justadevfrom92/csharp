# Project 7.6 — GitHub Actions CI Pipeline

`ci.yml` builds and tests every project in `/Projects` on each push and pull request.

## Switch it on

GitHub only runs workflows from `.github/workflows/` at the **repo root**:

```bash
mkdir -p .github/workflows && cp Projects/Section7_SecurityBackgroundTasksAndDeployment/Project7.6_GitHubActionsCi/ci.yml .github/workflows/ci.yml
```

## Stages

```
                         ┌──► e2e-tests      (Playwright, headless Chromium)
build-and-unit-tests ────┤
 restore → build → xUnit └──► nuget-package  (dotnet pack → run consumer app)
```

| Job | Steps | Why |
|---|---|---|
| `build-and-unit-tests` | `dotnet restore` → `dotnet build -c Release` (whole solution) → `dotnet test` Banking.Tests with coverage | Fast checks first. If the code doesn't even compile, don't spend minutes installing a browser. |
| `e2e-tests` | build → install Chromium (`playwright.ps1 install --with-deps`) → `dotnet test` Playwright | Slow, so it only runs after stage 1 is green. Headless: CI machines have no screen. |
| `nuget-package` | `dotnet pack` → `dotnet run` the consumer | Proves the package can really be installed and used, not just compiled. Runs in parallel with E2E. |

## Triggers

- `push` to `main` and every `pull_request` — but only when files under `Projects/` (or the workflow
  itself) change, so editing a text file in the root doesn't burn CI minutes.
- `workflow_dispatch` — a manual "Run workflow" button.

## Failure handling

- **Fail fast:** `needs:` stops later stages if stage 1 fails.
- **Results are always uploaded** (`if: always()`): `.trx` test reports and coverage files are kept
  as artifacts even when tests fail, so you can see *which* test failed and why.
- **Timeouts** on every job: a hung test (e.g. waiting for a browser forever) is killed after 15–20
  minutes instead of the default 6 hours.
- **`concurrency` + `cancel-in-progress`:** pushing a fix cancels the old run, so results always match
  the latest commit.
- **Recovery:** fix the code and push again, or click "Re-run failed jobs" for flaky network problems
  (e.g. a NuGet download timeout). Never "fix" a red build by skipping or deleting tests.

## Why these choices

- **`dotnet-version: 10.0.x`** installs the newest .NET 10 SDK, which matches `Projects/global.json`
  (10.0.100 or any newer feature band).
- **Release configuration** in CI — that's what ships, and some bugs only appear with optimisations on.
- **`--no-build` / `--no-restore`** after the first build, so nothing is compiled twice.
