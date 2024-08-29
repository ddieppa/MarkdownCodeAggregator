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
        IGitignoreParser gitignoreParser,
        ILogger logger)
        : ICodeAggregator
{
    public async Task<(string Content, int FileCount, int TotalTokens)> AggregateCodeAsync(
        string sourceDirectory,
        string outputDirectory,
        string? excludeFilePath,
        Action<string, double>? progressCallback = null)
    {
        var excludePatterns = await gitignoreParser.ParseAsync(excludeFilePath);
        logger.Information("Exclude patterns: {@Patterns}", excludePatterns);

        var files = GetCodeFiles(sourceDirectory, outputDirectory, excludePatterns).ToList();
        var fileCount = files.Count;
        var totalTokens = 0;
        var processedFileCount = 0;

        var aggregatedContent = new StringBuilder();
        aggregatedContent.AppendLine($"# Code Aggregation Report");
        aggregatedContent.AppendLine($"Source Directory: {sourceDirectory}");
        aggregatedContent.AppendLine($"Total Files Found: {fileCount}");

        var processedFiles = await Task.WhenAll(files.Select(async (file, index) =>
        {
            logger.Information("Processing file: {File}", file);
            var content = await fileSystem.ReadAllTextAsync(file);
            var nonEmptyLines = content.Split('\n')
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToArray();

            if (nonEmptyLines.Length > 0)
            {
                var cleanContent = string.Join('\n', nonEmptyLines);
                var relativePath = Path.GetRelativePath(sourceDirectory, file).Replace('\\', '/');
                var codeFile = new CodeFile(new FilePath(relativePath), cleanContent);
                var formattedContent = formatter.FormatCode(codeFile);
                var tokens = tokenCounter.CountTokens(cleanContent);

                progressCallback?.Invoke(Path.GetFileName(file), (index + 1.0) / fileCount);

                return (FormattedContent: formattedContent, Tokens: tokens);
            }

            return (FormattedContent: string.Empty, Tokens: 0);
        }));

        foreach (var (formattedContent, tokens) in processedFiles.Where(f => !string.IsNullOrEmpty(f.FormattedContent)))
        {
            aggregatedContent.Append(formattedContent);
            totalTokens += tokens;
            processedFileCount++;
        }

        aggregatedContent.Insert(aggregatedContent.ToString().IndexOf('\n') + 1,
            $"Total Files Processed: {processedFileCount}\n" +
            $"Total Tokens: {totalTokens}\n");

        return (aggregatedContent.ToString(), processedFileCount, totalTokens);
    }

    private IEnumerable<string> GetCodeFiles(string sourceDirectory, string outputDirectory, IEnumerable<string> excludePatterns)
    {
        var allFiles = fileSystem.GetFiles(sourceDirectory, "*.*", SearchOption.AllDirectories);
        logger.Information("Total files found: {Count}", allFiles.Count());
        return allFiles.Where(file => !ShouldIgnoreFile(file, sourceDirectory, outputDirectory, excludePatterns));
    }

    private bool ShouldIgnoreFile(string filePath, string baseDirectory, string outputDirectory, IEnumerable<string> patterns)
    {
        var relativePath = Path.GetRelativePath(baseDirectory, filePath).Replace('\\', '/');

        if (Path.GetFullPath(filePath).StartsWith(Path.GetFullPath(outputDirectory)))
        {
            logger.Information("File {File} is in output directory, ignoring", filePath);
            return true;
        }

        var shouldIgnore = gitignoreParser.ShouldIgnore(relativePath, patterns);
        logger.Information("File: {File}, RelativePath: {RelativePath}, ShouldIgnore: {ShouldIgnore}", filePath, relativePath, shouldIgnore);
        return shouldIgnore;
    }
}