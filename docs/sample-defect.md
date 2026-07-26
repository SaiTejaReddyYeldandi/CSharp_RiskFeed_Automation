# Example Defect Report

**Title:** PF-BETA dashboard marks feed PASS when PostgreSQL sequence 2005 is
missing

**Severity / priority:** High / P1

**Environment:** Compose integration environment, PostgreSQL 16, .NET 8 API,
Chromium

**Precondition:** Client has `risk:read` and PF-BETA contains sequences
2001-2004 and 2006-2008.

## Steps to reproduce

1. Send `GET /api/v1/risk-feed?portfolioId=PF-BETA&limit=50` with the required
   client and entitlement headers.
2. Observe `dataQuality.sequenceGapCount`.
3. Open the dashboard and select PF-BETA.
4. Compare the UI quality badge with a direct PostgreSQL sequence query.

## Expected

- API reports one sequence gap.
- Overall data-quality status is `Fail`.
- Dashboard shows `FAIL` and the sequence control requires review.

## Actual

- API reports zero gaps.
- Dashboard displays `PASS`.

## Evidence to attach

- Correlation ID from response header.
- JSON response and OpenAPI version.
- SQL query output ordered by `sequence_no`.
- Playwright screenshot/trace.
- Application and PostgreSQL container logs for the same timestamp.

## Root-cause hypothesis

Gap calculation is being performed after `ORDER BY ... LIMIT`, and a boundary
case may be comparing the wrong adjacent sequence pair.

## Regression coverage

Add a unit case with sequences 2004 and 2006, an integration fixture with the
same database state, and a UI assertion that the quality badge changes to
`FAIL`.
