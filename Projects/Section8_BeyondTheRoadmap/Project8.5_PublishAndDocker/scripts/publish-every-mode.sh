#!/usr/bin/env bash
# Publishes PublishDemo in every mode into publish/<mode>/, starts each one, calls /info, and prints the size.
# Usage (from the Project8.5 folder):  ./scripts/publish-every-mode.sh
set -euo pipefail
cd "$(dirname "$0")/.."
PROJECT=PublishDemo/PublishDemo.csproj
RID=linux-x64          # target platform: win-x64, osx-arm64, linux-arm64 ...
rm -rf publish

publish() {
    local mode="$1"; shift
    echo "== $mode: dotnet publish $PROJECT -c Release -o publish/$mode $*"
    dotnet publish "$PROJECT" -c Release -o "publish/$mode" "$@" > "publish-$mode.log" 2>&1 || { echo "   FAILED - see publish-$mode.log"; return; }
    rm -f "publish-$mode.log"
}

publish framework-dependent
publish self-contained       -r $RID --self-contained
publish single-file          -r $RID --self-contained -p:PublishSingleFile=true
publish trimmed              -r $RID --self-contained -p:PublishSingleFile=true -p:PublishTrimmed=true
publish ready-to-run         -r $RID --self-contained -p:PublishReadyToRun=true
publish native-aot           -r $RID -p:PublishAot=true

echo
printf "%-20s %8s %6s  %-6s %s\n" MODE SIZE FILES AOT "STARTS & ANSWERS /info?"
port=5900
for dir in publish/*/; do
    mode=$(basename "$dir")
    size=$(du -sh "$dir" | cut -f1)
    files=$(find "$dir" -type f | wc -l)
    if [ -f "$dir/PublishDemo" ]; then
        "$dir/PublishDemo" --urls "http://localhost:$port" > /dev/null 2>&1 &
    else
        dotnet "$dir/PublishDemo.dll" --urls "http://localhost:$port" > /dev/null 2>&1 &
    fi
    pid=$!
    info=""
    for attempt in $(seq 40); do
        info=$(curl -s "localhost:$port/info" || true)
        [ -n "$info" ] && break
        sleep 0.25
    done
    kill $pid 2>/dev/null || true
    aot=$(echo "$info" | grep -o '"isNativeAot":[a-z]*' | cut -d: -f2 || true)  # || true: no match must not stop the script
    printf "%-20s %8s %6s  %-6s %s\n" "$mode" "$size" "$files" "${aot:-?}" "$([ -n "$info" ] && echo yes || echo NO)"
    port=$((port + 1))
done
