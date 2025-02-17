using System.Text.RegularExpressions;

namespace DotNetMetadataMcpServer.Helpers;

public static class FilteringHelper
{
    public static string PrepareFilteringPattern(string filter)
    {
        return "^" + Regex.Escape(filter).Replace("\\*", ".*") + "$";
    }
    
    public static Predicate<string> PrepareFilteringPredicate(string filter)
    {
        var pattern = PrepareFilteringPattern(filter);
        return new Predicate<string>(input => Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase));
    }
}