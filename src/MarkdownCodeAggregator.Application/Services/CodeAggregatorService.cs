using MarkdownCodeAggregator.Application.DTOs;
using MarkdownCodeAggregator.Application.Interfaces;
using MarkdownCodeAggregator.Domain.Interfaces;
using Serilog;

namespace MarkdownCodeAggregator.Application.Services;

public class CodeAggregatorService(ICodeAggregator codeAggregator, ILogger logger) : ICodeAggregatorService
{
    public async Task<AggregationResult> AggregateCodeAsync(
            string sourceDirectory,
            string outputDirectory,
            string? excludeFilePath,
            Action<string, double>? progressCallback = null)
    {
        try
        {
            var (aggregatedContent, fileCount, totalTokens) = await codeAggregator.AggregateCodeAsync(
                                                                      sourceDirectory,
                                                                      outputDirectory,
                                                                      excludeFilePath,
                                                                      progressCallback);

            return new AggregationResult(aggregatedContent, totalTokens, fileCount);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error occurred during code aggregation");
            throw new ApplicationException("An error occurred during code aggregation. See inner exception for details.", ex);
        }
    }
}