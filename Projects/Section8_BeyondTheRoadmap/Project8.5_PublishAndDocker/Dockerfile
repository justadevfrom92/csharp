# Build from the Projects/ folder:  docker build -f Section8_BeyondTheRoadmap/Project8.5_PublishAndDocker/Dockerfile -t roadmap/publish-demo .
# Multi-stage build: stage 1 has the full SDK to BUILD, stage 2 has only the runtime to RUN.
# The final image never contains the SDK, the source code or build tools - smaller and safer.

# ---------- Stage 1: build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
# Copy only the project file first and restore: Docker caches this layer, so packages are only
# downloaded again when the .csproj changes - not on every code change.
# Build context = the Projects/ folder (see README), because every project gets its TargetFramework from
# Projects/Directory.Build.props - without it, restore fails with "TargetFramework value '' was not recognized".
COPY Directory.Build.props ./
COPY Section8_BeyondTheRoadmap/Project8.5_PublishAndDocker/PublishDemo/PublishDemo.csproj PublishDemo/
RUN dotnet restore PublishDemo/PublishDemo.csproj
COPY Section8_BeyondTheRoadmap/Project8.5_PublishAndDocker/PublishDemo/ PublishDemo/
RUN dotnet publish PublishDemo/PublishDemo.csproj -c Release -o /app --no-restore

# ---------- Stage 2: run ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .
# The image's built-in non-root user: if the app is ever broken into, the attacker isn't root.
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "PublishDemo.dll"]
