# AI-Assisted Testing Approach

This portfolio project used Codex/ChatGPT as an engineering accelerator for
test-risk brainstorming, scaffold review, and failure analysis. AI output was
treated as untrusted input, not as release evidence.

## Safe workflow

1. Start with explicit acceptance criteria and a requirement ID.
2. Ask AI for missing negative, boundary, data-quality, and concurrency risks.
3. Review generated code for correctness, security, maintainability, and fit
   with the existing architecture.
4. Compile with warnings as errors.
5. Run deterministic unit tests, real PostgreSQL integration tests, contract
   checks, browser tests, and performance thresholds.
6. Keep only behavior that can be explained and defended in code review.

No employer, client, personal, or production financial data was supplied. All
portfolio names, instruments, identifiers, and risk values are fictional.

## Example AI prompts

- "Given a timestamped portfolio risk feed with monotonic sequences, identify
  data-quality edge cases beyond null and range validation."
- "Review this OpenAPI change for consumer-breaking response or status-code
  differences."
- "Classify this CI failure as likely product, test, data, dependency, or
  environment, and list the evidence needed before changing code."
- "Suggest Playwright assertions that verify a responsive chart without relying
  on brittle CSS classes or fixed sleeps."

## Human-review checklist

- Can I explain the code path and trade-off without the AI transcript?
- Are SQL statements parameterized and permissions least-privilege?
- Does a test fail for the intended product defect?
- Is time, random data, and asynchronous behavior deterministic?
- Is the generated output covered by an independent oracle where possible?
- Did dependency, formatting, build, and security gates pass?

The project verification record documents the executable evidence used to
accept the final result.
