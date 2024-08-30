using MarkdownCodeAggregator.Application.DTOs;

namespace MarkdownCodeAggregator.Application.Interfaces;

public interface ICodeAggregatorService
{
    Task<AggregationResult> AggregateCodeAsync(string sourceDirectory,
            string outputDirectory,
            Action<string, double>? progressCallback = null);
}