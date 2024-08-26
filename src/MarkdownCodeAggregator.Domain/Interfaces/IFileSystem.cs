namespace MarkdownCodeAggregator.Domain.Interfaces;

public interface IFileSystem
{
    IEnumerable<string> GetFiles(string path, string searchPattern, SearchOption searchOption);
    Task<string> ReadAllTextAsync(string path);
    Task WriteAllTextAsync(string path, string contents);
    bool FileExists(string path);
}