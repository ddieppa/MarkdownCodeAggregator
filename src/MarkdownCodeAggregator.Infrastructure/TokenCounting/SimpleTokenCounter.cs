using MarkdownCodeAggregator.Domain.Interfaces;

namespace MarkdownCodeAggregator.Infrastructure.TokenCounting;

public class SimpleTokenCounter : ITokenCounter
{
    public int CountTokens(string text) =>
            text.Split([' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries).Length;
}