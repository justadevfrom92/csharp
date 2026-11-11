# Project 8.5 — `dotnet publish` (every mode) + Docker

| Path | What |
|---|---|
| `PublishDemo/` | A tiny API (`/info`, `/greet`, `/health`) written so it works in **every** publish mode, including Native AOT |
| `PublishDemo.Tests/` | Tests for the endpoints |
| `scripts/publish-every-mode.sh` | Publishes in all 6 modes, starts each one, checks it answers, prints sizes |
| `Dockerfile` | Normal multi-stage container image |
| `Dockerfile.aot` | Native AOT container image on a minimal "chiseled" base |

`GET /info` tells you how the running copy was published (`isNativeAot`, `isRunningInContainer`, executable path).

## `dotnet run` vs `dotnet build` vs `dotnet publish`

| Command | What it does | Use it for |
|---|---|---|
| `dotnet run` | build + start, from the source folder | developing |
| `dotnet build` | compile into `bin/` (keeps debug info, references the SDK's packages) | checking it compiles, running tests |
| `dotnet publish` | compile **and** collect **everything needed to run** into one folder | deploying to a server, a container, a customer |

## Every `dotnet publish` mode

Run from the `PublishDemo` folder (or pass the `.csproj` path). `-c Release` = optimised build; `-o <folder>` =
where to put the output; `-r <RID>` = target platform (`linux-x64`, `linux-arm64`, `win-x64`, `osx-arm64`).

**1. Framework-dependent** — needs .NET installed on the server; smallest; runs on any OS that has .NET:

```bash
dotnet publish -c Release -o publish/framework-dependent
```

Run it with `dotnet publish/framework-dependent/PublishDemo.dll`.

**2. Self-contained** — includes the .NET runtime; the server needs nothing installed:

```bash
dotnet publish -c Release -r linux-x64 --self-contained -o publish/self-contained
```

**3. Single file** — self-contained, packed into **one** executable:

```bash
dotnet publish -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true -o publish/single-file
```

**4. Trimmed** — single file, with unused code removed:

```bash
dotnet publish -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true -p:PublishTrimmed=true -o publish/trimmed
```

**5. ReadyToRun** — code pre-compiled to machine code (faster start, still JIT-capable):

```bash
dotnet publish -c Release -r linux-x64 --self-contained -p:PublishReadyToRun=true -o publish/ready-to-run
```

**6. Native AOT** — compiled **completely** to native machine code; no JIT, no runtime to install:

```bash
dotnet publish -c Release -r linux-x64 -p:PublishAot=true -o publish/native-aot
```

Needs a native toolchain on the build machine (clang on Linux, Visual Studio C++ tools on Windows).

**7. Container image, no Dockerfile needed** — the SDK builds the image itself:

```bash
dotnet publish -c Release -r linux-x64 /t:PublishContainer
```

That pushes `roadmap/publish-demo:1.0.0` (name and tag come from `<ContainerRepository>` / `<ContainerImageTag>`
in the `.csproj`) into the local Docker. No Docker? Save it as a file instead:

```bash
dotnet publish -c Release -r linux-x64 /t:PublishContainer -p:ContainerArchiveOutputPath=/tmp/publish-demo.tar
```

**Other useful options**

| Option | Effect |
|---|---|
| `--no-restore` / `--no-build` | Skip steps already done (CI pipelines) |
| `-p:Version=1.2.3` | Stamp a version number into the output |
| `-p:DebugType=none` | Leave out `.pdb` debug files |
| `-p:InvariantGlobalization=true` | Drop culture data (smaller; fine for APIs that don't format dates/money per culture) |
| `-p:PublishProfile=<name>` | Use saved settings from `Properties/PublishProfiles/<name>.pubxml` |
| `-p:ContainerBaseImage=mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled` | Different base image for `/t:PublishContainer` |

## Measured results (this Linux x64 container, `./scripts/publish-every-mode.sh`)

| Mode | Size | Files | Starts & answers `/info` | Startup to first answer | Memory after start |
|---|---|---|---|---|---|
| Framework-dependent | **204 KB** | 7 | yes | ~320 ms | ~60 MB |
| Self-contained | 106 MB | 334 | yes | ~240 ms | ~62 MB |
| Single file | 97 MB | 1 exe | yes | | |
| Trimmed (single file) | **22 MB** | 1 exe | yes (after the fix below) | | |
| ReadyToRun | 106 MB | 334 | yes | | |
| Native AOT | **16.5 MB** exe (+31 MB optional `.dbg` symbols) | 1 exe | yes, `isNativeAot: true` | **~45 ms** | **~25 MB** |

| Container image | Size | Runs as | Has a shell? |
|---|---|---|---|
| `Dockerfile` (aspnet base) | 342 MB | non-root user `app` | yes |
| `/t:PublishContainer` | 342 MB | non-root | yes |
| `Dockerfile.aot` (chiseled base) | **44.7 MB** | non-root (uid 1654) | **no** — nothing for an attacker to use |

## Which mode to choose

| Situation | Choose |
|---|---|
| Server already has .NET (or you use the official .NET container images) | **Framework-dependent** — tiny, patched with the OS/image |
| Server has no .NET, or must not depend on what's installed | **Self-contained** |
| You hand someone one program to run | **Single file** (+ **trimmed** if size matters) |
| Startup time matters (serverless, scale-to-zero, CLI tools), or memory/image size matters | **Native AOT** — if all your libraries support it |
| Containers | `/t:PublishContainer` for simplicity, a **Dockerfile** for full control |

**Native AOT's limits:** no runtime code generation, so no reflection-heavy libraries, no `dynamic`, no loading
plugins at runtime. This app is AOT-ready because of three settings:
`IsAotCompatible` (the compiler warns about anything that would break), `EnableRequestDelegateGenerator`
(endpoint code generated at compile time), and source-generated JSON (`Serialization/AppJsonSerializerContext.cs`).
`WebApplication.CreateSlimBuilder` starts with fewer features to keep it small.

## Problems found while building this — and the fixes

1. **Trimmed app crashed on start** with `TypeLoadException: Method 'GetEnumerator' in type
   'X509Certificate2Collection' ... does not have an implementation` — the trimmer removed a method Kestrel uses.
   Fix in the `.csproj`: `<TrimmerRootAssembly Include="System.Security.Cryptography" />` (never trim that assembly).
   Still 22 MB instead of 97 MB. (Native AOT trims differently and didn't need it.)
2. **Docker build couldn't find the target framework** (`TargetFramework value '' was not recognized`): the
   framework is set in `Projects/Directory.Build.props`, which is **outside** the project folder, and Docker can only
   see files inside its build context. Fix: build from the `Projects/` folder and `COPY Directory.Build.props`.
3. **The AOT Dockerfile silently built a normal app:** `dotnet restore` without `-p:PublishAot=true` never downloads
   the native compiler, and the `--no-restore` publish then quietly skipped AOT. Fix: pass `-p:PublishAot=true` to
   **restore** as well.
4. *(This sandbox only)* restore inside `docker build` failed with `UntrustedRoot`, because this cloud environment
   routes traffic through a proxy with its own certificate. On a normal machine or GitHub's CI runners this doesn't
   happen; the images above were verified with a one-off build that trusted that certificate, and the committed
   Dockerfiles don't contain anything environment-specific.

## Run the containers

Build from the **`Projects/`** folder (so `Directory.Build.props` is inside the build context):

```bash
docker build -f Section8_BeyondTheRoadmap/Project8.5_PublishAndDocker/Dockerfile -t roadmap/publish-demo .
```

```bash
docker build -f Section8_BeyondTheRoadmap/Project8.5_PublishAndDocker/Dockerfile.aot -t roadmap/publish-demo:aot .
```

```bash
docker run --rm -p 8080:8080 roadmap/publish-demo:aot
```

```bash
curl localhost:8080/info
```

## Tests (every behaviour has all three kinds)

| Behaviour | PositivePositive | PositiveNegative | NegativeNegative |
|---|---|---|---|
| `/greet` | `name=Ada` → "Hello, Ada!" | 51 characters → 400 (exactly 50 → 200) | no name, empty, only spaces → 400 |
| `/info` | reports .NET 10, OS, executable path | in tests: honestly says **not** AOT and **not** a container | `POST /info` → 405, `/information` → 404 |

The publish modes themselves are checked by `scripts/publish-every-mode.sh` (each output must start and answer).
