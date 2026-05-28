namespace WeatherApp.Server.Triage;

public enum ForecastSeverity
{
    Low,
    Moderate,
    High
}

public sealed record ForecastTriageDecision(ForecastSeverity Severity, string TriageStatus);

public static class ForecastTriageEvaluator
{
    private static readonly string[] EscalationKeywords = ["blizzard", "flood", "hail", "tornado"];

    public static ForecastSeverity MapSeverity(int temperatureC) => temperatureC switch
    {
        <= -10 => ForecastSeverity.High,
        < 0 => ForecastSeverity.Moderate,
        <= 34 => ForecastSeverity.Low,
        < 40 => ForecastSeverity.Moderate,
        _ => ForecastSeverity.High
    };

    public static bool HasEscalationKeyword(string? summary)
    {
        if (string.IsNullOrWhiteSpace(summary))
        {
            return false;
        }

        return EscalationKeywords.Any(keyword =>
            summary.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    public static ForecastTriageDecision Evaluate(int temperatureC, string? summary)
    {
        var severity = MapSeverity(temperatureC);
        var triageStatus = severity == ForecastSeverity.High || HasEscalationKeyword(summary)
            ? "escalated"
            : "new";

        return new ForecastTriageDecision(severity, triageStatus);
    }
}