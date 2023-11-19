using System.Text.RegularExpressions;

namespace webapi.Extensions;

public static class StringExtensions
{
    const string DEFAULT_SEPARATOR = ";";
    const string DEFAULT_ASSIGNATOR = "=";

    public static Dictionary<string, string>? ToDictionary(
        this string str
        , string? separator = DEFAULT_SEPARATOR, string? assignator = DEFAULT_ASSIGNATOR
        , StringComparer? stringComparer = null
    ) {
        Dictionary<string, string>? ret = null;

        string s = separator ?? DEFAULT_SEPARATOR;
        string a = assignator ?? DEFAULT_ASSIGNATOR;
        string regexp = @$"\s*([^{a}{s}\s]+?)\s*{a}\s*([^{a}{s}]*?)\s*({s}|$)+";

        ret = Regex.Matches(str, regexp)
            .OfType<Match>()
            .ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value, stringComparer);

        return ret;
    }
}
