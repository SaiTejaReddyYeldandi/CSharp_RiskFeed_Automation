# Contributing

1. Create a focused branch.
2. Link the change to a requirement in the traceability matrix.
3. Add tests at the lowest layer that proves the behavior.
4. Run formatting, build, unit, and relevant integration/UI tests.
5. Keep secrets and production data out of commits.
6. Describe risk, evidence, and rollback considerations in the pull request.

The build treats warnings as errors. A change is not complete when it only
passes on a developer machine; the CI quality gate is the release signal.
