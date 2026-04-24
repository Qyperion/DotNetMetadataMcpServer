using System.Text.RegularExpressions;

namespace DotNetMetadataMcpServer.Helpers;

public static class FilteringHelper
{
    public static string PrepareFilteringPattern(string filter)
    {
        return "^" + Regex.Escape(filter).Replace("\\*", ".*") + "$";
    }

    public static Predicate<string> PrepareFilteringPredicate(string filter, bool caseSensitive = false)
    {
        var pattern = PrepareFilteringPattern(filter);
        var options = caseSensitive
            ? RegexOptions.Compiled
            : RegexOptions.IgnoreCase | RegexOptions.Compiled;
        var regex = new Regex(pattern, options);
        return input => regex.IsMatch(input);
    }
}
