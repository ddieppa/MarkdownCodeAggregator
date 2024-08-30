﻿using System.Text;
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
        Action<string, double>? progressCallback = null)
    {
        var files = await GetCodeFilesAsync(sourceDirectory, outputDirectory);
        var aggregatedContent = new StringBuilder();
        var fileCount = files.Count;
        var processedFileCount = 0;
        var totalTokens = 0;

        aggregatedContent.AppendLine("# Code Aggregation Report");
        aggregatedContent.AppendLine($"Source Directory: {sourceDirectory}");
        aggregatedContent.AppendLine($"Total Files Found: {fileCount}");
        aggregatedContent.AppendLine();

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
                    aggregatedContent.Append(formatter.FormatCode(codeFile));
                    processedFileCount++;
                    totalTokens += tokenCounter.CountTokens(cleanContent);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error processing file: {File}", file);
                aggregatedContent.AppendLine($"## Error processing file: {file}");
                aggregatedContent.AppendLine($"```");
                aggregatedContent.AppendLine(ex.ToString());
                aggregatedContent.AppendLine($"```");
            }

            progressCallback?.Invoke(Path.GetFileName(file), (i + 1.0) / fileCount);
        }

        aggregatedContent.AppendLine($"Total Files Processed: {processedFileCount}");
        aggregatedContent.AppendLine($"Total Tokens: {totalTokens}");

        return (aggregatedContent.ToString(), processedFileCount, totalTokens);
    }

    private async Task<List<string>> GetCodeFilesAsync(string sourceDirectory, string outputDirectory)
    {
        var allFiles = await fileFilter.GetTrackedFilesAsync(sourceDirectory);
        logger.Information("Total files found: {Count}", allFiles.Count());

        var includedFiles = new List<string>();
        foreach (var file in allFiles)
        {
            if (await ShouldIncludeFileAsync(file, sourceDirectory, outputDirectory))
            {
                includedFiles.Add(file);
            }
        }

        return includedFiles;
    }

    private async Task<bool> ShouldIncludeFileAsync(string filePath, string sourceDirectory, string outputDirectory)
    {
        if (Path.GetFullPath(filePath).StartsWith(Path.GetFullPath(outputDirectory)))
        {
            logger.Information("File {File} is in output directory, ignoring", filePath);
            return false;
        }

        return await fileFilter.ShouldIncludeFileAsync(filePath, sourceDirectory);
    }
}