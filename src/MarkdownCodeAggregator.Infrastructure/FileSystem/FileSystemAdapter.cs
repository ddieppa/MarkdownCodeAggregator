

using MarkdownCodeAggregator.Domain.Interfaces;

namespace MarkdownCodeAggregator.Infrastructure.FileSystem;

public class FileSystemAdapter : IFileSystem
{
    public IEnumerable<string> GetFiles(string path, string searchPattern, SearchOption searchOption) =>
            Directory.GetFiles(path, searchPattern, searchOption);

    public Task<string> ReadAllTextAsync(string path) => File.ReadAllTextAsync(path);

    public Task WriteAllTextAsync(string path, string contents) => File.WriteAllTextAsync(path, contents);

    public bool FileExists(string path) => File.Exists(path);
}