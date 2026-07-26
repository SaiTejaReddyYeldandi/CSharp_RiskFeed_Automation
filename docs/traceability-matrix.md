# Requirement-to-Test Traceability

| ID | Acceptance criterion | Automated evidence |
|---|---|---|
| RF-001 | An entitled client can retrieve a portfolio feed ordered by descending sequence | `FeedApiTests.GetFeed_WithReadEntitlement_ReturnsOrderedQualityCheckedData` |
| RF-002 | A missing client identity returns 401 Problem Details | `FeedApiTests.GetFeed_WithoutClientIdentity_ReturnsUnauthorizedProblem` |
| RF-003 | A client without `risk:read` receives 403 | `FeedApiTests.GetFeed_WithoutRequiredEntitlement_ReturnsForbidden` |
| RF-004 | Feed limit is constrained to 1-500 | Unit limit theories and `FeedApiTests.GetFeed_WithInvalidLimit_ReturnsBadRequest` |
| DQ-001 | Required identifiers and codes use approved formats | `RiskEventValidatorTests` invalid-field theories |
| DQ-002 | VaR and Expected Shortfall cannot be negative | `Validate_WithNegativeLossMetric_RejectsValue` |
| DQ-003 | Signed Delta Exposure is allowed | `Validate_WithNegativeDelta_AllowsSignedExposure` |
| DQ-004 | Events older than 30 days or over two minutes in the future are rejected | Timestamp validator tests |
| DQ-005 | Sequence gaps and duplicates change quality status to Fail | `DataQualitySummaryTests.Evaluate_WithGapAndDuplicate_ReturnsFail` |
| DB-001 | API value, event ID, and sequence match PostgreSQL | `DatabaseValidationTests.ApiLatestEvent_MatchesPostgresSourceOfTruth` |
| DB-002 | Database constraints reject duplicate portfolio sequences | `DuplicatePortfolioSequence_IsRejectedByDatabase` |
| DB-003 | Seeded records satisfy format, range, time, and sequence rules | `SeededData_SatisfiesDatabaseQualityConstraints` |
| CT-001 | OpenAPI exposes versioned read/write routes and 201/409/422 responses | `ContractTests.OpenApiContract_ContainsVersionedFeedOperationsAndResponses` |
| UI-001 | Dashboard renders the risk chart and passing data-quality controls | `Dashboard_LoadsRiskChartAndPassingQualityControls` |
| UI-002 | Selecting PF-BETA refreshes the table from the API | `PortfolioSelection_RefreshesChartFromRiskFeedApi` |
| UI-003 | Dashboard controls remain visible at 390 x 844 with no page overflow | `MobileViewport_RendersControlsWithoutHorizontalOverflow` |
| PF-001 | Baseline feed reads remain below 1% errors and 300 ms p95 | NBomber `risk_feed_read` runtime thresholds |
