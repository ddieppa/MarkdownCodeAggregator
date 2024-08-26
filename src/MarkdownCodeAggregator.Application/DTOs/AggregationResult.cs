namespace MarkdownCodeAggregator.Application.DTOs;

public record AggregationResult(string AggregatedContent, int TokenCount, int FileCount);