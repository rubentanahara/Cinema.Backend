#!/usr/bin/env bash
# Exports each subgraph's SDL to Src/Services/<Service>/schema.graphql
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
port=5199
for svc in Catalog Seating Pricing Ordering Payments Ticketing Loyalty Concessions Identity Notifications; do
  proj="$root/Src/Services/$svc/Cinema.$svc.csproj"
  ASPNETCORE_ENVIRONMENT=Development dotnet run --project "$proj" --urls "http://localhost:$port" >/dev/null 2>&1 &
  pid=$!
  if curl -s --retry 60 --retry-delay 1 --retry-connrefused --max-time 60 \
       "http://localhost:$port/graphql/schema.graphql" -o "$root/Src/Services/$svc/schema.graphql"; then
    printf '%-14s %s lines\n' "$svc" "$(wc -l < "$root/Src/Services/$svc/schema.graphql" | tr -d ' ')"
  else
    echo "$svc FAILED" >&2
  fi
  kill "$pid" 2>/dev/null || true
  wait "$pid" 2>/dev/null || true
done
