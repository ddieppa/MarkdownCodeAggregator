using MarkdownCodeAggregator.Application.DTOs;
using MarkdownCodeAggregator.Application.Interfaces;
using MarkdownCodeAggregator.Domain.Interfaces;

namespace MarkdownCodeAggregator.Application.Services;

public class CodeAggregatorService(ICodeAggregator codeAggregator, ITokenCounter tokenCounter, IFileSystem fileSystem) : ICodeAggregatorService
{
    public async Task<AggregationResult> AggregateCodeAsync(string sourceDirectory, string? excludeFile, Action<string, double>? progressCallback = null)
    {
        var excludedFolders = await GetExcludedFoldersAsync(excludeFile);
        var (aggregatedContent, fileCount) = await codeAggregator.AggregateCodeAsync(sourceDirectory, excludedFolders, progressCallback);
        var tokenCount = tokenCounter.CountTokens(aggregatedContent);

        return new AggregationResult(aggregatedContent, tokenCount, fileCount);
    }

    private async Task<IEnumerable<string>> GetExcludedFoldersAsync(string? excludeFile) =>
            !string.IsNullOrEmpty(excludeFile) && fileSystem.FileExists(excludeFile)
                    ? (await fileSystem.ReadAllTextAsync(excludeFile)).Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                    : [];
}