# Architecture Decisions

## ADR-001: Separate the domain rules from transport and storage

Risk validation and quality evaluation live in `RiskFeed.Core`. They do not
depend on ASP.NET Core, Npgsql, Playwright, or NBomber. This keeps the most
important financial-data rules fast to test and reusable by future REST,
streaming, or gRPC adapters.

## ADR-002: Use Npgsql directly

Parameterized SQL keeps the persistence behavior visible to a QA automation
reviewer and makes precision, ordering, constraint, and mapping checks explicit.
The project is small enough that an ORM would add abstraction without reducing
risk.

## ADR-003: Use Testcontainers for database integration

A real disposable PostgreSQL instance catches behavior that mocks cannot:
timestamp types, numeric precision, constraints, indexes, conflict semantics,
and SQL mapping.

## ADR-004: Keep demo authorization replaceable

`X-Client-Id` and `X-Entitlements` make authorization paths self-contained for
a public portfolio repository. The endpoint filter is intentionally isolated;
a production OAuth2/JWT policy can replace it without changing handlers or
domain services.

## ADR-005: Test the production UI artifact

Docker builds the React application and serves it from the ASP.NET Core runtime.
Playwright therefore tests the same compiled static assets and same-origin API
topology that the container exposes, including mobile layout behavior.

## ADR-006: Treat performance thresholds as release criteria

NBomber uses an open arrival model so a slow service does not quietly reduce
the requested load. Error percentage and p95 latency are executable thresholds,
not prose-only targets.
