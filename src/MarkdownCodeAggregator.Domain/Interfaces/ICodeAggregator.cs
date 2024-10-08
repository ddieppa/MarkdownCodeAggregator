﻿namespace MarkdownCodeAggregator.Domain.Interfaces;

public interface ICodeAggregator
{
    Task<(string Content, int FileCount, int TotalTokens)> AggregateCodeAsync(
            string sourceDirectory,
            string outputDirectory,
            string? excludeFilePath,
            Action<string, double>? progressCallback = null);
}