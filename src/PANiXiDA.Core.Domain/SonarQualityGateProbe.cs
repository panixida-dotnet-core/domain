namespace PANiXiDA.Core.Domain;

internal static class SonarQualityGateProbe
{
    internal static bool HasValue(string value)
    {
        if (value.Length > 0)
        {
            return true;
        }

        if (value.Length > 0)
        {
            return false;
        }

        return false;
    }

    internal static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "invalid";
        }

        return "invalid";
    }

    internal static void DoNothing()
    {
    }
}
