CREATE TABLE IF NOT EXISTS risk_events (
    event_id UUID PRIMARY KEY,
    portfolio_id VARCHAR(32) NOT NULL,
    instrument VARCHAR(32) NOT NULL,
    metric VARCHAR(32) NOT NULL
        CHECK (metric IN ('ValueAtRisk95', 'ExpectedShortfall', 'DeltaExposure')),
    risk_value NUMERIC(20, 6) NOT NULL
        CHECK (ABS(risk_value) <= 1000000000),
    currency CHAR(3) NOT NULL
        CHECK (currency ~ '^[A-Z]{3}$'),
    as_of_utc TIMESTAMPTZ NOT NULL,
    sequence_no BIGINT NOT NULL CHECK (sequence_no > 0),
    source VARCHAR(64) NOT NULL,
    ingested_at_utc TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT uq_risk_events_portfolio_sequence
        UNIQUE (portfolio_id, sequence_no)
);

CREATE INDEX IF NOT EXISTS ix_risk_events_portfolio_as_of
    ON risk_events (portfolio_id, as_of_utc DESC);

WITH seed (
    event_id,
    portfolio_id,
    instrument,
    metric,
    risk_value,
    currency,
    sequence_no,
    source,
    seconds_ago
) AS (
    VALUES
        ('10000000-0000-0000-0000-000000000001'::uuid, 'PF-ALPHA', 'EURUSD', 'ValueAtRisk95', 118250.000000, 'USD', 1001, 'risk-engine-v1', 220),
        ('10000000-0000-0000-0000-000000000002'::uuid, 'PF-ALPHA', 'EURUSD', 'ValueAtRisk95', 121700.000000, 'USD', 1002, 'risk-engine-v1', 200),
        ('10000000-0000-0000-0000-000000000003'::uuid, 'PF-ALPHA', 'EURUSD', 'ValueAtRisk95', 119900.000000, 'USD', 1003, 'risk-engine-v1', 180),
        ('10000000-0000-0000-0000-000000000004'::uuid, 'PF-ALPHA', 'EURUSD', 'ValueAtRisk95', 124800.000000, 'USD', 1004, 'risk-engine-v1', 160),
        ('10000000-0000-0000-0000-000000000005'::uuid, 'PF-ALPHA', 'EURUSD', 'ValueAtRisk95', 127400.000000, 'USD', 1005, 'risk-engine-v1', 140),
        ('10000000-0000-0000-0000-000000000006'::uuid, 'PF-ALPHA', 'EURUSD', 'ValueAtRisk95', 126150.000000, 'USD', 1006, 'risk-engine-v1', 120),
        ('10000000-0000-0000-0000-000000000007'::uuid, 'PF-ALPHA', 'EURUSD', 'ValueAtRisk95', 130600.000000, 'USD', 1007, 'risk-engine-v1', 100),
        ('10000000-0000-0000-0000-000000000008'::uuid, 'PF-ALPHA', 'EURUSD', 'ValueAtRisk95', 128900.000000, 'USD', 1008, 'risk-engine-v1', 80),
        ('10000000-0000-0000-0000-000000000009'::uuid, 'PF-ALPHA', 'EURUSD', 'ValueAtRisk95', 133200.000000, 'USD', 1009, 'risk-engine-v1', 60),
        ('10000000-0000-0000-0000-000000000010'::uuid, 'PF-ALPHA', 'EURUSD', 'ValueAtRisk95', 136450.000000, 'USD', 1010, 'risk-engine-v1', 40),
        ('20000000-0000-0000-0000-000000000001'::uuid, 'PF-BETA', 'XAUUSD', 'ValueAtRisk95', 84200.000000, 'USD', 2001, 'risk-engine-v1', 180),
        ('20000000-0000-0000-0000-000000000002'::uuid, 'PF-BETA', 'XAUUSD', 'ValueAtRisk95', 81950.000000, 'USD', 2002, 'risk-engine-v1', 160),
        ('20000000-0000-0000-0000-000000000003'::uuid, 'PF-BETA', 'XAUUSD', 'ValueAtRisk95', 86700.000000, 'USD', 2003, 'risk-engine-v1', 140),
        ('20000000-0000-0000-0000-000000000004'::uuid, 'PF-BETA', 'XAUUSD', 'ValueAtRisk95', 88450.000000, 'USD', 2004, 'risk-engine-v1', 120),
        ('20000000-0000-0000-0000-000000000005'::uuid, 'PF-BETA', 'XAUUSD', 'ValueAtRisk95', 87300.000000, 'USD', 2005, 'risk-engine-v1', 100),
        ('20000000-0000-0000-0000-000000000006'::uuid, 'PF-BETA', 'XAUUSD', 'ValueAtRisk95', 91200.000000, 'USD', 2006, 'risk-engine-v1', 80),
        ('20000000-0000-0000-0000-000000000007'::uuid, 'PF-BETA', 'XAUUSD', 'ValueAtRisk95', 93150.000000, 'USD', 2007, 'risk-engine-v1', 60),
        ('20000000-0000-0000-0000-000000000008'::uuid, 'PF-BETA', 'XAUUSD', 'ValueAtRisk95', 92500.000000, 'USD', 2008, 'risk-engine-v1', 40)
)
INSERT INTO risk_events (
    event_id,
    portfolio_id,
    instrument,
    metric,
    risk_value,
    currency,
    as_of_utc,
    sequence_no,
    source,
    ingested_at_utc
)
SELECT
    event_id,
    portfolio_id,
    instrument,
    metric,
    risk_value,
    currency,
    CURRENT_TIMESTAMP - (seconds_ago * INTERVAL '1 second'),
    sequence_no,
    source,
    CURRENT_TIMESTAMP
FROM seed
ON CONFLICT (event_id) DO UPDATE SET
    risk_value = EXCLUDED.risk_value,
    as_of_utc = EXCLUDED.as_of_utc,
    ingested_at_utc = EXCLUDED.ingested_at_utc;
