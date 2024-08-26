namespace MarkdownCodeAggregator.Domain.Interfaces;

public interface ITokenCounter
{
    int CountTokens(string text);
}