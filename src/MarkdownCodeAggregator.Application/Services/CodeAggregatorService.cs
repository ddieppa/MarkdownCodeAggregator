using MarkdownCodeAggregator.Application.DTOs;
using MarkdownCodeAggregator.Application.Interfaces;
using MarkdownCodeAggregator.Domain.Interfaces;

namespace MarkdownCodeAggregator.Application.Services;

public class CodeAggregatorService(ICodeAggregator codeAggregator) : ICodeAggregatorService
{
    public async Task<AggregationResult> AggregateCodeAsync(string sourceDirectory, string outputDirectory,
            string? excludeFilePath, Action<string, double>? progressCallback = null)
    {
        var (aggregatedContent, fileCount, totalTokens) = await codeAggregator.AggregateCodeAsync(sourceDirectory,
                                                              outputDirectory,
                                                              excludeFilePath,
                                                              progressCallback);
        return new AggregationResult(aggregatedContent, totalTokens, fileCount);
    }
}