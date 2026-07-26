using System.Text.RegularExpressions;
using RiskFeed.Core.Abstractions;
using RiskFeed.Core.Domain;
using RiskFeed.Core.Models;

namespace RiskFeed.Core.Services;

public sealed partial class RiskEventValidator(TimeProvider timeProvider) : IRiskEventValidator
{
    private const decimal MaximumAbsoluteValue = 1_000_000_000m;
    private static readonly TimeSpan MaximumFutureSkew = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan MaximumAge = TimeSpan.FromDays(30);

    public IReadOnlyList<DataQualityIssue> Validate(IngestRiskEventRequest request)
    {
        List<DataQualityIssue> issues = [];
        DateTimeOffset nowUtc = timeProvider.GetUtcNow();

        ValidateRequiredCode(
            request.PortfolioId,
            "portfolioId",
            32,
            PortfolioPattern(),
            issues);
        ValidateRequiredCode(
            request.Instrument,
            "instrument",
            32,
            InstrumentPattern(),
            issues);
        ValidateRequiredCode(
            request.Currency,
            "currency",
            3,
            CurrencyPattern(),
            issues);
        ValidateRequiredCode(
            request.Source,
            "source",
            64,
            SourcePattern(),
            issues);

        if (!Enum.IsDefined(request.Metric))
        {
            issues.Add(new(
                "metric",
                "unsupported_metric",
                "Metric must be a supported risk metric."));
        }

        if (request.Sequence <= 0)
        {
            issues.Add(new(
                "sequence",
                "invalid_sequence",
                "Sequence must be greater than zero."));
        }

        if (Math.Abs(request.Value) > MaximumAbsoluteValue)
        {
            issues.Add(new(
                "value",
                "out_of_range",
                "Absolute risk value must not exceed 1,000,000,000."));
        }

        if (request.Metric is RiskMetric.ValueAtRisk95 or RiskMetric.ExpectedShortfall
            && request.Value < 0)
        {
            issues.Add(new(
                "value",
                "negative_loss_measure",
                "Value at Risk and Expected Shortfall must be non-negative."));
        }

        if (request.AsOfUtc == default)
        {
            issues.Add(new(
                "asOfUtc",
                "required",
                "As-of timestamp is required."));
        }
        else if (request.AsOfUtc > nowUtc + MaximumFutureSkew)
        {
            issues.Add(new(
                "asOfUtc",
                "future_timestamp",
                "As-of timestamp cannot be more than two minutes in the future."));
        }
        else if (request.AsOfUtc < nowUtc - MaximumAge)
        {
            issues.Add(new(
                "asOfUtc",
                "stale_timestamp",
                "As-of timestamp cannot be older than 30 days."));
        }

        return issues;
    }

    private static void ValidateRequiredCode(
        string? value,
        string field,
        int maximumLength,
        Regex pattern,
        List<DataQualityIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            issues.Add(new(field, "required", $"{field} is required."));
            return;
        }

        if (value.Length > maximumLength)
        {
            issues.Add(new(
                field,
                "max_length",
                $"{field} must not exceed {maximumLength} characters."));
        }

        if (!pattern.IsMatch(value))
        {
            issues.Add(new(
                field,
                "invalid_format",
                $"{field} has an invalid format."));
        }
    }

    [GeneratedRegex("^[A-Z0-9][A-Z0-9-]*$", RegexOptions.CultureInvariant)]
    private static partial Regex PortfolioPattern();

    [GeneratedRegex("^[A-Z0-9][A-Z0-9._/-]*$", RegexOptions.CultureInvariant)]
    private static partial Regex InstrumentPattern();

    [GeneratedRegex("^[A-Z]{3}$", RegexOptions.CultureInvariant)]
    private static partial Regex CurrencyPattern();

    [GeneratedRegex("^[a-z0-9][a-z0-9._-]*$", RegexOptions.CultureInvariant)]
    private static partial Regex SourcePattern();
}
