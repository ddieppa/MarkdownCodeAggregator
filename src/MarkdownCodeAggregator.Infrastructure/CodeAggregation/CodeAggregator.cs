using MarkdownCodeAggregator.Domain.Entities;
using MarkdownCodeAggregator.Domain.Interfaces;
using MarkdownCodeAggregator.Domain.ValueObjects;

namespace MarkdownCodeAggregator.Infrastructure.CodeAggregation;

public class CodeAggregator(IFileSystem fileSystem, IFormatter formatter) : ICodeAggregator
{
    public async Task<(string Content, int FileCount)> AggregateCodeAsync(string sourceDirectory, IEnumerable<string> excludedFolders, Action<string, double>? progressCallback = null)
    {
        var files = GetCodeFiles(sourceDirectory, excludedFolders).ToList();
        var aggregatedContent = new System.Text.StringBuilder();
        var fileCount = files.Count;
        var processedFileCount = 0;

        aggregatedContent.AppendLine($"# Code Aggregation Report");
        aggregatedContent.AppendLine($"Source Directory: {sourceDirectory}");
        aggregatedContent.AppendLine($"Total Files Found: {fileCount}");
        aggregatedContent.AppendLine();

        for (int i = 0; i < fileCount; i++)
        {
            var file = files[i];
            var content = await fileSystem.ReadAllTextAsync(file);

            if (!string.IsNullOrWhiteSpace(content))
            {
                var codeFile = new CodeFile(new FilePath(file), content);
                aggregatedContent.Append(formatter.FormatCode(codeFile));
                processedFileCount++;
            }

            progressCallback?.Invoke(Path.GetFileName(file), (i + 1.0) / fileCount);
        }

        aggregatedContent.AppendLine($"Total Files Processed: {processedFileCount}");

        return (aggregatedContent.ToString(), processedFileCount);
    }

    private IEnumerable<string> GetCodeFiles(string sourceDirectory, IEnumerable<string> excludedFolders) =>
            fileSystem.GetFiles(sourceDirectory, "*.*", SearchOption.AllDirectories)
                    .Where(file => !excludedFolders.Any(file.Contains));
}