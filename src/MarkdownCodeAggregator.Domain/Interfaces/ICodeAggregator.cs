namespace MarkdownCodeAggregator.Domain.Interfaces;


public interface ICodeAggregator
{
    Task<(string Content, int FileCount)> AggregateCodeAsync(string sourceDirectory, IEnumerable<string> excludedFolders, Action<string, double>? progressCallback = null);
}