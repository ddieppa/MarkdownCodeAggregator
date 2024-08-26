using MarkdownCodeAggregator.Application.DTOs;

namespace MarkdownCodeAggregator.Application.Interfaces;

public interface ICodeAggregatorService
{
    Task<AggregationResult> AggregateCodeAsync(string sourceDirectory, string? excludeFile, Action<string, double>? progressCallback = null);
}