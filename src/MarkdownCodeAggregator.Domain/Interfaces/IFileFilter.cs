namespace MarkdownCodeAggregator.Domain.Interfaces;

public interface IFileFilter
{
    Task<IEnumerable<string>> GetTrackedFilesAsync(string directory);
    Task<bool> ShouldIncludeFileAsync(string filePath, string baseDirectory, string? excludeFilePath);
}