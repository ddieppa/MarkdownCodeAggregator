using System.Text.RegularExpressions;
using MarkdownCodeAggregator.Domain.Interfaces;

public class AdvancedTokenCounter : ITokenCounter
{
    private static readonly Regex TokenRegex = new(@"\w+|[^\w\s]", RegexOptions.Compiled);

    public int CountTokens(string text)
    {
        var matches = TokenRegex.Matches(text);
        return matches.Count;
    }
}