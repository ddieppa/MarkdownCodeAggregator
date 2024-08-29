using System.Text;
using System.Text.RegularExpressions;
using MarkdownCodeAggregator.Domain.Interfaces;
using Serilog;

namespace MarkdownCodeAggregator.Infrastructure.FileSystem;

public class GitignoreParser(IFileSystem fileSystem, ILogger logger) : IGitignoreParser
{
    private static readonly string[] GitFolderPatterns = { ".git", ".git/**" };

    public async Task<IEnumerable<string>> ParseAsync(string? excludeFilePath)
    {
        if (string.IsNullOrEmpty(excludeFilePath) || !fileSystem.FileExists(excludeFilePath))
        {
            logger.Information("No valid exclude file found at {ExcludeFilePath}", excludeFilePath);
            return Enumerable.Empty<string>();
        }

        var content = await fileSystem.ReadAllTextAsync(excludeFilePath);
        var patterns = content.Split('\n')
            .Where(line => !string.IsNullOrWhiteSpace(line) && !line.TrimStart().StartsWith('#'))
            .Select(line => line.Trim())
            .ToList();

        logger.Information("Parsed {PatternCount} patterns from {ExcludeFilePath}", patterns.Count, excludeFilePath);
        return patterns;
    }

    public bool ShouldIgnore(string relativePath, IEnumerable<string> patterns)
    {
        relativePath = relativePath.Replace('\\', '/').TrimStart('/');

        // Always check .git folder patterns first
        if (GitFolderPatterns.Any(pattern => MatchesPattern(relativePath, pattern)))
        {
            logger.Debug("Path {RelativePath} ignored due to .git folder pattern", relativePath);
            return true;
        }

        var shouldIgnore = false;
        logger.Debug("Checking if path should be ignored: {RelativePath}", relativePath);

        foreach (var pattern in patterns)
        {
            var isNegation = pattern.StartsWith("!");
            var actualPattern = isNegation ? pattern[1..] : pattern;
            var isMatch = MatchesPattern(relativePath, actualPattern);
            logger.Debug("Pattern: {Pattern}, IsNegation: {IsNegation}, IsMatch: {IsMatch}", pattern, isNegation, isMatch);

            if (isMatch)
            {
                shouldIgnore = !isNegation;
                logger.Debug("Updated shouldIgnore to {ShouldIgnore} based on pattern {Pattern}", shouldIgnore, pattern);
            }
        }

        logger.Information("Final decision for {RelativePath}: {ShouldIgnore}", relativePath, shouldIgnore);
        return shouldIgnore;
    }

    private bool MatchesPattern(string path, string pattern)
    {
        var regex = new Regex(ConvertGitignoreToRegex(pattern), RegexOptions.IgnoreCase);
        var isMatch = regex.IsMatch(path);
        logger.Debug("Matching path: {Path} against pattern: {Pattern}, result: {IsMatch}", path, pattern, isMatch);
        return isMatch;
    }

    private string ConvertGitignoreToRegex(string pattern)
    {
        var regexPattern = new StringBuilder("^");
        pattern = pattern.Replace('\\', '/');

        if (pattern.StartsWith("/"))
        {
            pattern = pattern.TrimStart('/');
        }
        else
        {
            regexPattern.Append("(?:.*/)?");
        }

        for (int i = 0; i < pattern.Length; i++)
        {
            char c = pattern[i];
            switch (c)
            {
                case '*' when i + 1 < pattern.Length && pattern[i + 1] == '*':
                    regexPattern.Append("(?:.*/)?"); // Match zero or more directories
                    i++;
                    break;
                case '*':
                    regexPattern.Append("[^/]*");
                    break;
                case '?':
                    regexPattern.Append("[^/]");
                    break;
                case '[':
                    var closeBracketIndex = pattern.IndexOf(']', i);
                    if (closeBracketIndex != -1)
                    {
                        var charClass = pattern.Substring(i, closeBracketIndex - i + 1);
                        regexPattern.Append(ConvertCharClassToRegex(charClass));
                        i = closeBracketIndex;
                    }
                    else
                    {
                        regexPattern.Append("\\[");
                    }
                    break;
                case '.':
                    regexPattern.Append("\\.");
                    break;
                case '/':
                    regexPattern.Append("/");
                    break;
                default:
                    regexPattern.Append(Regex.Escape(c.ToString()));
                    break;
            }
        }

        if (!pattern.EndsWith("/"))
            regexPattern.Append("(?:$|/.*)"); // Match end of string or trailing slash and anything after
        else
            regexPattern.Append("(/.*)?");  // Match optional trailing slash and anything after

        var result = regexPattern.ToString();
        logger.Debug("Converted gitignore pattern: {Pattern} to regex: {Regex}", pattern, result);
        return result;
    }

    private string ConvertCharClassToRegex(string charClass)
    {
        var sb = new StringBuilder("[");
        for (int i = 1; i < charClass.Length - 1; i++)
        {
            char c = charClass[i];
            if (char.IsLetter(c))
            {
                sb.Append(char.ToLower(c));
                sb.Append(char.ToUpper(c));
            }
            else
            {
                sb.Append(c);
            }
        }
        sb.Append("]");
        return sb.ToString();
    }
}