# Project 7.6 — GitHub Actions CI Pipeline

The workflow file is **`.github/workflows/ci.yml` at the repo root** — the only place GitHub looks for
workflows. It builds and tests every project in `/Projects` on each push and pull request.
Results: the repo's **Actions** tab on GitHub.

## Stages

```
                         ┌──► e2e-tests      (Playwright, headless Chromium)
build-and-unit-tests ────┤
 restore → build → xUnit └──► nuget-package  (dotnet pack → run consumer app)
```

| Job | Steps | Why |
|---|---|---|
| `build-and-unit-tests` | `dotnet restore` → `dotnet build -c Release` (whole solution) → `dotnet test` on **every** `*.Tests` project (found automatically, with coverage) | Fast checks first. If the code doesn't even compile, don't spend minutes installing a browser. All test projects run even if one fails, so you see every failure. |
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

## Code snippets

From `.github/workflows/ci.yml` at the repo root.

**When it runs, and cancelling older runs**

```yaml
on:
  push:
    branches: [main]
    paths: ["Projects/**", ".github/workflows/ci.yml"]
  pull_request:
    paths: ["Projects/**", ".github/workflows/ci.yml"]
  workflow_dispatch: # "Run workflow" button in the Actions tab

concurrency:
  group: ci-${{ github.ref }}
  cancel-in-progress: true
```

**A job: check out, install .NET, restore, build**

```yaml
build-and-unit-tests:
  runs-on: ubuntu-latest
  defaults:
    run:
      working-directory: Projects
  steps:
    - uses: actions/checkout@v4

    - uses: actions/setup-dotnet@v4
      with:
        dotnet-version: 10.0.x

    - name: Restore NuGet packages
      run: dotnet restore CSharpMasteryRoadmap.slnx

    - name: Build (Release)
      run: dotnet build CSharpMasteryRoadmap.slnx -c Release --no-restore
```

**Run every test project, even if one fails, then fail the job**

```yaml
- name: All xUnit test projects
  run: |
    failed=0
    for project in $(find . -name '*.Tests.csproj' -not -path '*/obj/*' | sort); do
      dotnet test "$project" -c Release --no-build --logger trx --results-directory TestResults || failed=1
    done
    exit $failed
```

**Keep the results, even when tests fail**

```yaml
- name: Upload unit test results
  if: always()
  uses: actions/upload-artifact@v4
  with:
    name: unit-test-results
    path: Projects/TestResults
```

**A job that only starts after another passes**

```yaml
e2e-tests:
  needs: build-and-unit-tests
  runs-on: ubuntu-latest
```
