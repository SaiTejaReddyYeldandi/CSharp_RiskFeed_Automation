# Verification Evidence

Run date: 26 July 2026

| Gate | Result |
|---|---|
| Release build | Passed, 0 warnings / 0 errors |
| `dotnet format --verify-no-changes` | Passed, 0 files changed |
| xUnit domain/service suite | 22 passed / 0 failed |
| Core line coverage | 91.48% (gate: 80%) |
| API/OpenAPI/PostgreSQL suite | 10 passed / 0 failed |
| C# Playwright suite | 3 passed / 0 failed |
| React TypeScript check and production build | Passed |
| NuGet transitive vulnerability audit | No known vulnerable packages |
| Docker API and PostgreSQL health | Healthy |
| NBomber 5-second smoke | 50 requests, 0 failures, 10 RPS |
| NBomber smoke latency | p50 4.36 ms, p95 58.59 ms, p99 115.46 ms |

The performance result is a local smoke observation on the development
machine. It is recorded as build evidence, not represented as a production
capacity claim. GitHub Actions runs the configurable baseline profile and
retains the generated NBomber HTML report.
