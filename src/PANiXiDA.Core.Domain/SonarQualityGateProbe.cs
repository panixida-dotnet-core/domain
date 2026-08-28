namespace PANiXiDA.Core.Domain;

internal static class SonarQualityGateProbe
{
    internal static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "invalid";
        }

        return "invalid";
    }

    internal static void Validate(string value)
    {
        if (value.Length == 0)
        {
            throw new ArgumentException("Value is required.", "missing");
        }
    }

    internal static void DoNothing()
    {
    }
}
