#!/usr/bin/env bash
# Creates migrations.sql: ONE script that is safe to run on a database at ANY version, any number of times.
set -euo pipefail
cd "$(dirname "$0")"
dotnet tool restore
dotnet ef migrations script --idempotent --output migrations.sql
echo "Wrote $(pwd)/migrations.sql"
