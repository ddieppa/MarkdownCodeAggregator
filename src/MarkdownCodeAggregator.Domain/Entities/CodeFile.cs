using MarkdownCodeAggregator.Domain.ValueObjects;

namespace MarkdownCodeAggregator.Domain.Entities;

public record CodeFile(FilePath Path, string Content);