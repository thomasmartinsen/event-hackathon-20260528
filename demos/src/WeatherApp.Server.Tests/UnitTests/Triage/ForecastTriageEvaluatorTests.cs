using WeatherApp.Server.Triage;
using Xunit;

namespace WeatherApp.Server.Tests.UnitTests.Triage;

public sealed class ForecastTriageEvaluatorTests
{
    [Theory]
    [InlineData(-15, ForecastSeverity.High)]
    [InlineData(-1, ForecastSeverity.Moderate)]
    [InlineData(20, ForecastSeverity.Low)]
    [InlineData(38, ForecastSeverity.Moderate)]
    [InlineData(44, ForecastSeverity.High)]
    public void MapSeverity_UsesExpectedTemperatureBands(int temperatureC, ForecastSeverity expectedSeverity)
    {
        var severity = ForecastTriageEvaluator.MapSeverity(temperatureC);

        Assert.Equal(expectedSeverity, severity);
    }

    [Fact]
    public void Evaluate_EscalatesWhenSummaryContainsEscalationKeyword()
    {
        var decision = ForecastTriageEvaluator.Evaluate(18, "Light rain with possible hail later");

        Assert.Equal(ForecastSeverity.Low, decision.Severity);
        Assert.Equal("escalated", decision.TriageStatus);
    }

    [Fact]
    public void Evaluate_DoesNotEscalateForWhitespaceSummaryAtLowSeverity()
    {
        var decision = ForecastTriageEvaluator.Evaluate(22, "   ");

        Assert.Equal(ForecastSeverity.Low, decision.Severity);
        Assert.Equal("new", decision.TriageStatus);
    }
}