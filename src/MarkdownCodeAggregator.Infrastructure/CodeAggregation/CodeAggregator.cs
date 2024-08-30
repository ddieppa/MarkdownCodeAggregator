using System.Text;
using MarkdownCodeAggregator.Domain.Interfaces;
using MarkdownCodeAggregator.Domain.Entities;
using MarkdownCodeAggregator.Domain.ValueObjects;
using Serilog;

namespace MarkdownCodeAggregator.Infrastructure.CodeAggregation;

public class CodeAggregator(
    IFileSystem fileSystem,
    IFormatter formatter,
    ITokenCounter tokenCounter,
    IFileFilter fileFilter,
    ILogger logger) : ICodeAggregator
{
    public async Task<(string Content, int FileCount, int TotalTokens)> AggregateCodeAsync(
        string sourceDirectory,
        string outputDirectory,
        string? excludeFilePath,
        Action<string, double>? progressCallback = null)
    {
        var files = await GetCodeFilesAsync(sourceDirectory, outputDirectory, excludeFilePath);
        var aggregatedContent = new StringBuilder();
        var fileCount = files.Count;
        var processedFileCount = 0;
        var totalTokens = 0;

        var codeContent = new StringBuilder();

        for (int i = 0; i < fileCount; i++)
        {
            var file = files[i];
            logger.Information("Processing file: {File}", file);

            try
            {
                var content = await fileSystem.ReadAllTextAsync(file);
                var nonEmptyLines = content.Split('\n')
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .ToArray();

                if (nonEmptyLines.Length > 0)
                {
                    var cleanContent = string.Join('\n', nonEmptyLines);
                    var relativePath = Path.GetRelativePath(sourceDirectory, file);
                    var codeFile = new CodeFile(new FilePath(relativePath), cleanContent);
                    codeContent.Append(formatter.FormatCode(codeFile));
                    processedFileCount++;
                    totalTokens += tokenCounter.CountTokens(cleanContent);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error processing file: {File}", file);
                codeContent.AppendLine($"## Error processing file: {file}");
                codeContent.AppendLine($"```");
                codeContent.AppendLine(ex.ToString());
                codeContent.AppendLine($"```");
            }

            progressCallback?.Invoke(Path.GetFileName(file), (i + 1.0) / fileCount);
        }

        // Add report information at the beginning
        aggregatedContent.AppendLine("# Code Aggregation Report");
        aggregatedContent.AppendLine($"Source Directory: {sourceDirectory}");
        aggregatedContent.AppendLine($"Total Files Found: {fileCount}");
        aggregatedContent.AppendLine($"Total Files Processed: {processedFileCount}");
        aggregatedContent.AppendLine($"Total Tokens: {totalTokens}");
        aggregatedContent.AppendLine();

        // Append the code content
        aggregatedContent.Append(codeContent);

        return (aggregatedContent.ToString(), processedFileCount, totalTokens);
    }

    private async Task<List<string>> GetCodeFilesAsync(string sourceDirectory, string outputDirectory, string? excludeFilePath)
    {
        var allFiles = await fileFilter.GetTrackedFilesAsync(sourceDirectory);
        logger.Information("Total files found: {Count}", allFiles.Count());

        var includedFiles = new List<string>();
        foreach (var file in allFiles)
        {
            if (await ShouldIncludeFileAsync(file, sourceDirectory, outputDirectory, excludeFilePath))
            {
                includedFiles.Add(file);
            }
        }

        return includedFiles;
    }

    private async Task<bool> ShouldIncludeFileAsync(string filePath, string sourceDirectory, string outputDirectory, string? excludeFilePath)
    {
        if (Path.GetFullPath(filePath).StartsWith(Path.GetFullPath(outputDirectory)))
        {
            logger.Information("File {File} is in output directory, ignoring", filePath);
            return false;
        }

        return await fileFilter.ShouldIncludeFileAsync(filePath, sourceDirectory, excludeFilePath);
    }
}