# Risk Feed Test Strategy

## Objective

Provide fast, repeatable evidence that authorized clients receive complete,
correct, current risk events and that regressions are blocked before release.

## Primary quality risks

1. A missing or duplicated sequence silently distorts a chart.
2. The API and PostgreSQL values diverge through mapping or precision errors.
3. A client reads or writes a portfolio feed without the required entitlement.
4. A breaking response change reaches the React visualization.
5. Stale or malformed events are accepted as current risk.
6. A dashboard works on desktop but clips controls on mobile web.
7. Latency or error rate degrades under a realistic arrival rate.

## Test layers

| Layer | Scope | Isolation | Release signal |
|---|---|---|---|
| Unit | Validator, quality summary, orchestration | In-memory repository and fixed clock | Every push |
| Integration | HTTP, OpenAPI, middleware, service, Npgsql, schema | Disposable PostgreSQL Testcontainer | Every push |
| Data | API-to-SQL reconciliation and constraints | Direct independent SQL queries | Every push |
| UI | React chart, filters, quality indicators, responsive layout | Production container in Chromium | Every pull request |
| Performance | Authorized risk-feed read path | Compose stack with NBomber arrival model | Weekly / on demand |

## Test data design

- Seed data is fictional and deterministic by identity and sequence.
- Timestamps refresh when the demo database initializes so freshness behavior
  is meaningful.
- Write tests use a separate `PF-GAMMA` portfolio and deterministic event ID.
- Integration tests run against a disposable database and leave no shared
  environment state.
- Direct constraint tests use a transaction and roll it back.

## Contract strategy

The service publishes OpenAPI 3 at `/swagger/v1/swagger.json`. Automated checks
assert that versioned collection and item routes exist and that create,
validation, and conflict responses remain declared. Typed deserialization
checks the runtime JSON contract separately.

For a multi-team production topology, the next step would publish the OpenAPI
artifact and use consumer-driven contracts (for example Pact) for dashboard and
downstream feed consumers.

## Entry and exit criteria

Entry:

- Requirement has acceptance criteria and a traceability ID.
- Code compiles locally with warnings as errors.
- Required test data can be created without production information.

Exit:

- Unit line coverage is at least 80%.
- No unit, integration, data, contract, or required Playwright test fails.
- No unresolved critical or high-severity defect remains.
- NBomber error rate is below 1% and p95 is below 300 ms for the baseline
  profile.
- TRX, container logs, coverage, and performance reports are retained by CI.

## Failure triage

1. Capture correlation ID, test name, environment, commit, and timestamps.
2. Classify the failure as product, test, data, environment, or dependency.
3. Reproduce at the lowest reliable test layer.
4. Compare API payload, application logs, and direct PostgreSQL state.
5. File a defect with expected/actual behavior and attach evidence.
6. Add or refine a regression test before closure.

Flaky tests are defects. Retries are not enabled as a blanket workaround;
event-based Playwright waits, disposable infrastructure, and fixed clocks are
used to remove nondeterminism at its source.
