namespace MarkdownCodeAggregator.Domain.Interfaces;

public interface IGitignoreParser
{
    Task<IEnumerable<string>> ParseAsync(string? excludeFilePath);
    bool ShouldIgnore(string relativePath, IEnumerable<string> patterns);
}