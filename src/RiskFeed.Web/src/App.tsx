import { useCallback, useEffect, useMemo, useState } from "react";

type QualityStatus = "Pass" | "Warning" | "Fail";

type RiskEvent = {
  eventId: string;
  portfolioId: string;
  instrument: string;
  metric: string;
  value: number;
  currency: string;
  asOfUtc: string;
  sequence: number;
  source: string;
  ingestedAtUtc: string;
};

type DataQualitySummary = {
  status: QualityStatus;
  recordCount: number;
  sequenceGapCount: number;
  duplicateSequenceCount: number;
  staleRecordCount: number;
  latestRecordAgeSeconds: number | null;
};

type RiskFeedPage = {
  items: RiskEvent[];
  dataQuality: DataQualitySummary;
  generatedAtUtc: string;
};

const portfolios = ["PF-ALPHA", "PF-BETA"] as const;
const currencyFormatter = new Intl.NumberFormat("en-US", {
  style: "currency",
  currency: "USD",
  maximumFractionDigits: 0
});

function App() {
  const [portfolio, setPortfolio] = useState<(typeof portfolios)[number]>(
    "PF-ALPHA"
  );
  const [feed, setFeed] = useState<RiskFeedPage | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadFeed = useCallback(async (signal?: AbortSignal) => {
    setLoading(true);
    setError(null);

    try {
      const response = await fetch(
        `/api/v1/risk-feed?portfolioId=${encodeURIComponent(portfolio)}&limit=50`,
        {
          headers: {
            "X-Client-Id": "risk-dashboard",
            "X-Entitlements": "risk:read"
          },
          signal
        }
      );

      if (!response.ok) {
        throw new Error(`Risk feed request failed with status ${response.status}.`);
      }

      const payload = (await response.json()) as RiskFeedPage;
      setFeed(payload);
    } catch (requestError) {
      if (requestError instanceof DOMException && requestError.name === "AbortError") {
        return;
      }

      setError(
        requestError instanceof Error
          ? requestError.message
          : "The risk feed could not be loaded."
      );
    } finally {
      if (!signal?.aborted) {
        setLoading(false);
      }
    }
  }, [portfolio]);

  useEffect(() => {
    const controller = new AbortController();
    void loadFeed(controller.signal);
    return () => controller.abort();
  }, [loadFeed]);

  const latestEvent = feed?.items[0];
  const change = useMemo(() => {
    if (!feed || feed.items.length < 2) {
      return 0;
    }

    return feed.items[0].value - feed.items[1].value;
  }, [feed]);

  return (
    <div className="app-shell">
      <header className="topbar">
        <a className="brand" href="/" aria-label="RiskScope home">
          <span className="brand-mark" aria-hidden="true">
            RS
          </span>
          <span>
            <strong>RiskScope</strong>
            <small>Feed assurance</small>
          </span>
        </a>
        <div className="environment">
          <span className="environment-dot" aria-hidden="true" />
          DEMO ENVIRONMENT
        </div>
      </header>

      <main>
        <section className="hero">
          <div>
            <p className="eyebrow">INTRADAY RISK MONITORING</p>
            <h1>Risk feed control room</h1>
            <p className="hero-copy">
              Live exposure visibility with automated freshness, sequence, and
              duplicate checks.
            </p>
          </div>

          <div className="toolbar">
            <label htmlFor="portfolio">Portfolio</label>
            <select
              id="portfolio"
              data-testid="portfolio-select"
              value={portfolio}
              onChange={(event) =>
                setPortfolio(event.target.value as (typeof portfolios)[number])
              }
            >
              {portfolios.map((item) => (
                <option value={item} key={item}>
                  {item}
                </option>
              ))}
            </select>
            <button
              className="refresh-button"
              type="button"
              onClick={() => void loadFeed()}
              disabled={loading}
            >
              {loading ? "Refreshing..." : "Refresh data"}
            </button>
          </div>
        </section>

        {error && (
          <div className="error-banner" role="alert">
            <strong>Feed unavailable.</strong> {error}
          </div>
        )}

        <section className="metric-grid" aria-label="Risk feed summary">
          <MetricCard
            label="Latest VaR 95"
            value={latestEvent ? currencyFormatter.format(latestEvent.value) : "—"}
            hint={
              latestEvent
                ? `${change >= 0 ? "+" : ""}${currencyFormatter.format(change)} from prior tick`
                : "Awaiting data"
            }
            accent
            testId="latest-risk-value"
          />
          <MetricCard
            label="Data quality"
            value={feed?.dataQuality.status.toUpperCase() ?? "CHECKING"}
            hint={`${feed?.dataQuality.sequenceGapCount ?? 0} gaps · ${
              feed?.dataQuality.duplicateSequenceCount ?? 0
            } duplicates`}
            status={feed?.dataQuality.status}
            testId="quality-status"
          />
          <MetricCard
            label="Feed freshness"
            value={
              feed?.dataQuality.latestRecordAgeSeconds != null
                ? `${Math.round(feed.dataQuality.latestRecordAgeSeconds)}s`
                : "—"
            }
            hint={`${feed?.dataQuality.staleRecordCount ?? 0} stale records`}
            testId="feed-freshness"
          />
          <MetricCard
            label="Validated records"
            value={String(feed?.dataQuality.recordCount ?? 0)}
            hint={`${portfolio} · ${latestEvent?.source ?? "risk engine"}`}
            testId="validated-records"
          />
        </section>

        <section className="content-grid">
          <article className="panel chart-panel">
            <div className="panel-heading">
              <div>
                <p className="eyebrow">VALUE AT RISK 95%</p>
                <h2>Exposure trend</h2>
              </div>
              <span className="metric-chip">{latestEvent?.currency ?? "USD"}</span>
            </div>
            {loading && !feed ? (
              <div className="chart-skeleton" aria-label="Loading risk chart" />
            ) : (
              <RiskChart events={feed?.items ?? []} />
            )}
          </article>

          <aside className="panel quality-panel">
            <div className="panel-heading">
              <div>
                <p className="eyebrow">AUTOMATED CONTROLS</p>
                <h2>Quality checks</h2>
              </div>
            </div>
            <QualityCheck
              name="Sequence continuity"
              detail="No missing feed positions"
              passed={(feed?.dataQuality.sequenceGapCount ?? 0) === 0}
            />
            <QualityCheck
              name="Duplicate detection"
              detail="Portfolio sequence is unique"
              passed={(feed?.dataQuality.duplicateSequenceCount ?? 0) === 0}
            />
            <QualityCheck
              name="Freshness threshold"
              detail="Latest tick is under 10 minutes"
              passed={(feed?.dataQuality.latestRecordAgeSeconds ?? Infinity) < 600}
            />
            <QualityCheck
              name="Contract validation"
              detail="Typed API payload accepted"
              passed={Boolean(feed)}
            />
          </aside>
        </section>

        <section className="panel table-panel">
          <div className="panel-heading">
            <div>
              <p className="eyebrow">AUDITABLE FEED EVENTS</p>
              <h2>Latest observations</h2>
            </div>
            <time dateTime={feed?.generatedAtUtc}>
              {feed
                ? `Updated ${new Date(feed.generatedAtUtc).toLocaleTimeString()}`
                : "Waiting for feed"}
            </time>
          </div>
          <div className="table-scroll">
            <table data-testid="risk-events-table">
              <thead>
                <tr>
                  <th scope="col">Sequence</th>
                  <th scope="col">Instrument</th>
                  <th scope="col">Metric</th>
                  <th scope="col">Value</th>
                  <th scope="col">As of (UTC)</th>
                  <th scope="col">Source</th>
                </tr>
              </thead>
              <tbody>
                {(feed?.items ?? []).slice(0, 8).map((item) => (
                  <tr key={item.eventId}>
                    <td className="mono">#{item.sequence}</td>
                    <td>{item.instrument}</td>
                    <td>{formatMetric(item.metric)}</td>
                    <td className="value-cell">
                      {currencyFormatter.format(item.value)}
                    </td>
                    <td>{new Date(item.asOfUtc).toLocaleTimeString()}</td>
                    <td>
                      <span className="source-pill">{item.source}</span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>
      </main>

      <footer>
        <span>RiskScope QA reference system</span>
        <span>Entitlement: risk:read</span>
      </footer>
    </div>
  );
}

type MetricCardProps = {
  label: string;
  value: string;
  hint: string;
  accent?: boolean;
  status?: QualityStatus;
  testId: string;
};

function MetricCard({
  label,
  value,
  hint,
  accent,
  status,
  testId
}: MetricCardProps) {
  return (
    <article className={`metric-card ${accent ? "metric-card-accent" : ""}`}>
      <p>{label}</p>
      <strong
        data-testid={testId}
        className={status ? `status-${status.toLowerCase()}` : undefined}
      >
        {value}
      </strong>
      <small>{hint}</small>
    </article>
  );
}

function QualityCheck({
  name,
  detail,
  passed
}: {
  name: string;
  detail: string;
  passed: boolean;
}) {
  return (
    <div className="quality-check">
      <span className={passed ? "check-pass" : "check-fail"} aria-hidden="true">
        {passed ? "✓" : "!"}
      </span>
      <span>
        <strong>{name}</strong>
        <small>{detail}</small>
      </span>
      <em>{passed ? "PASS" : "REVIEW"}</em>
    </div>
  );
}

function RiskChart({ events }: { events: RiskEvent[] }) {
  const points = [...events].reverse();

  if (points.length === 0) {
    return <div className="empty-state">No risk events matched this portfolio.</div>;
  }

  const values = points.map((item) => item.value);
  const minimum = Math.min(...values);
  const maximum = Math.max(...values);
  const range = Math.max(maximum - minimum, 1);
  const width = 760;
  const height = 270;
  const paddingX = 30;
  const paddingY = 24;
  const chartWidth = width - paddingX * 2;
  const chartHeight = height - paddingY * 2;
  const coordinates = points.map((item, index) => {
    const x =
      paddingX + (index / Math.max(points.length - 1, 1)) * chartWidth;
    const y = paddingY + ((maximum - item.value) / range) * chartHeight;
    return { x, y, item };
  });
  const line = coordinates
    .map((point, index) => `${index === 0 ? "M" : "L"}${point.x},${point.y}`)
    .join(" ");
  const area = `${line} L${coordinates.at(-1)!.x},${height - paddingY} L${
    coordinates[0].x
  },${height - paddingY} Z`;

  return (
    <div className="chart-wrap" data-testid="risk-chart">
      <svg
        viewBox={`0 0 ${width} ${height}`}
        role="img"
        aria-labelledby="risk-chart-title risk-chart-description"
      >
        <title id="risk-chart-title">Risk exposure trend</title>
        <desc id="risk-chart-description">
          A line chart showing the latest Value at Risk observations for the
          selected portfolio.
        </desc>
        {[0, 0.25, 0.5, 0.75, 1].map((ratio) => {
          const y = paddingY + ratio * chartHeight;
          return (
            <line
              key={ratio}
              x1={paddingX}
              x2={width - paddingX}
              y1={y}
              y2={y}
              className="grid-line"
            />
          );
        })}
        <defs>
          <linearGradient id="area-gradient" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor="#1ad6a3" stopOpacity="0.28" />
            <stop offset="100%" stopColor="#1ad6a3" stopOpacity="0" />
          </linearGradient>
        </defs>
        <path d={area} fill="url(#area-gradient)" />
        <path d={line} className="risk-line" />
        {coordinates.map(({ x, y, item }) => (
          <circle key={item.eventId} cx={x} cy={y} r="4" className="risk-point">
            <title>
              {currencyFormatter.format(item.value)} at{" "}
              {new Date(item.asOfUtc).toLocaleTimeString()}
            </title>
          </circle>
        ))}
      </svg>
      <div className="chart-labels" aria-hidden="true">
        <span>{new Date(points[0].asOfUtc).toLocaleTimeString()}</span>
        <span>{new Date(points.at(-1)!.asOfUtc).toLocaleTimeString()}</span>
      </div>
    </div>
  );
}

function formatMetric(metric: string) {
  return metric
    .replace("ValueAtRisk", "VaR ")
    .replace("ExpectedShortfall", "Expected Shortfall")
    .replace("DeltaExposure", "Delta Exposure");
}

export default App;
