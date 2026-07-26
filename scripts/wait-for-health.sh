#!/usr/bin/env sh
set -eu

url="${1:-http://localhost:8088/health/ready}"
attempts="${HEALTH_ATTEMPTS:-40}"
delay_seconds="${HEALTH_DELAY_SECONDS:-2}"
attempt=1

while [ "$attempt" -le "$attempts" ]; do
  if curl --fail --silent "$url" > /dev/null; then
    echo "Risk feed is healthy at $url"
    exit 0
  fi

  echo "Health attempt $attempt/$attempts has not succeeded yet."
  attempt=$((attempt + 1))
  sleep "$delay_seconds"
done

echo "Risk feed did not become healthy at $url." >&2
exit 1
