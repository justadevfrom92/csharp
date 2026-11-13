# Cheat Sheet — `dotnet` command line

## Create

```bash
dotnet new list                          # every template
dotnet new console -n MyApp
dotnet new web -n MyApi                  # empty web app (minimal APIs)
dotnet new webapi -n MyApi
dotnet new mvc -n MySite
dotnet new razor -n MyPages
dotnet new blazor -n MyBlazor
dotnet new grpc -n MyGrpc
dotnet new classlib -n MyLibrary
dotnet new xunit -n MyApp.Tests
dotnet new sln -n MySolution             # .slnx in .NET 10
dotnet new gitignore
```

## Solutions and references

```bash
dotnet sln add MyApp/MyApp.csproj
dotnet sln list
dotnet add MyApp.Tests reference MyApp/MyApp.csproj
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
dotnet add package Serilog.AspNetCore --version 9.0.0
dotnet remove package Serilog.AspNetCore
dotnet list package --outdated
```

## Build, run, test

```bash
dotnet restore
dotnet build
dotnet build -c Release
dotnet run
dotnet run --project MyApp
dotnet run -c Release -- --some-arg      # args after -- go to the app
dotnet watch                             # rebuild + restart on every save
dotnet test
dotnet test --filter "FullyQualifiedName~BankAccountTest"
dotnet test --filter "DisplayName~PositiveNegative"
dotnet test --collect:"XPlat Code Coverage"
dotnet clean
dotnet format                            # fix code style
```

## Environment and settings

```bash
ASPNETCORE_ENVIRONMENT=Development dotnet run
dotnet run --urls http://localhost:5050
Shop__ShopName="Test" dotnet run         # override appsettings: section__key

dotnet user-secrets init
dotnet user-secrets set "Jwt:SigningKey" "a-long-secret"
dotnet user-secrets list
```

## Tools and EF Core

```bash
dotnet new tool-manifest
dotnet tool install dotnet-ef
dotnet tool restore

dotnet ef migrations add InitialCreate
dotnet ef migrations list
dotnet ef migrations remove
dotnet ef database update
dotnet ef database update PreviousMigrationName   # go back
dotnet ef migrations script --idempotent -o migrations.sql
dotnet ef dbcontext scaffold "Data Source=shop.db" Microsoft.EntityFrameworkCore.Sqlite -o Models   # from an existing database
```

## Package and publish

```bash
dotnet pack -c Release
dotnet nuget push bin/Release/MyLibrary.1.0.0.nupkg --source https://api.nuget.org/v3/index.json --api-key <key>

dotnet publish -c Release -o out
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
dotnet publish -c Release -r linux-x64 -p:PublishAot=true
```

## Information

```bash
dotnet --version
dotnet --list-sdks
dotnet --list-runtimes
dotnet --info
dotnet dev-certs https --trust           # trust the local HTTPS certificate
```
