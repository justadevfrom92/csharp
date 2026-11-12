# Project 8.5 — `dotnet publish` (every mode)

| Path | What |
|---|---|
| `PublishDemo/` | A tiny API (`/info`, `/greet`, `/health`) written so it works in **every** publish mode, including Native AOT |
| `PublishDemo.Tests/` | Tests for the endpoints |
| `scripts/publish-every-mode.sh` | Publishes in all 6 modes, starts each one, checks it answers, prints sizes |

`GET /info` tells you how the running copy was published (`isNativeAot`, framework, executable path).

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

**Other useful options**

| Option | Effect |
|---|---|
| `--no-restore` / `--no-build` | Skip steps already done (CI pipelines) |
| `-p:Version=1.2.3` | Stamp a version number into the output |
| `-p:DebugType=none` | Leave out `.pdb` debug files |
| `-p:InvariantGlobalization=true` | Drop culture data (smaller; fine for APIs that don't format dates/money per culture) |
| `-p:PublishProfile=<name>` | Use saved settings from `Properties/PublishProfiles/<name>.pubxml` |

## Measured results (Linux x64, `./scripts/publish-every-mode.sh`)

| Mode | Size | Files | Starts & answers `/info` | Startup to first answer | Memory after start |
|---|---|---|---|---|---|
| Framework-dependent | **204 KB** | 7 | yes | ~320 ms | ~60 MB |
| Self-contained | 106 MB | 334 | yes | ~240 ms | ~62 MB |
| Single file | 97 MB | 1 exe | yes | | |
| Trimmed (single file) | **22 MB** | 1 exe | yes (after the fix below) | | |
| ReadyToRun | 106 MB | 334 | yes | | |
| Native AOT | **16.5 MB** exe (+31 MB optional `.dbg` symbols) | 1 exe | yes, `isNativeAot: true` | **~45 ms** | **~25 MB** |

## Which mode to choose

| Situation | Choose |
|---|---|
| Server already has .NET installed | **Framework-dependent** — tiny, patched with the OS/image |
| Server has no .NET, or must not depend on what's installed | **Self-contained** |
| You hand someone one program to run | **Single file** (+ **trimmed** if size matters) |
| Startup time matters (serverless, scale-to-zero, CLI tools), or memory/image size matters | **Native AOT** — if all your libraries support it |

**Native AOT's limits:** no runtime code generation, so no reflection-heavy libraries, no `dynamic`, no loading
plugins at runtime. This app is AOT-ready because of three settings:
`IsAotCompatible` (the compiler warns about anything that would break), `EnableRequestDelegateGenerator`
(endpoint code generated at compile time), and source-generated JSON (`SerializerContexts/AppJsonSerializerContext.cs`).
`WebApplication.CreateSlimBuilder` starts with fewer features to keep it small.

## Problems found while building this — and the fixes

1. **Trimmed app crashed on start** with `TypeLoadException: Method 'GetEnumerator' in type
   'X509Certificate2Collection' ... does not have an implementation` — the trimmer removed a method Kestrel uses.
   Fix in the `.csproj`: `<TrimmerRootAssembly Include="System.Security.Cryptography" />` (never trim that assembly).
   Still 22 MB instead of 97 MB. (Native AOT trims differently and didn't need it.)

## Tests (every behaviour has all three kinds)

| Behaviour | PositivePositive | PositiveNegative | NegativeNegative |
|---|---|---|---|
| `/greet` | `name=Ada` → "Hello, Ada!" | 51 characters → 400 (exactly 50 → 200) | no name, empty, only spaces → 400 |
| `/info` | reports .NET 10, OS, executable path | in tests: honestly says **not** Native AOT | `POST /info` → 405, `/information` → 404 |

The publish modes themselves are checked by `scripts/publish-every-mode.sh` (each output must start and answer).
