using MarkdownCodeAggregator.Domain.Entities;

namespace MarkdownCodeAggregator.Domain.Interfaces;

public interface IFormatter
{
    string FormatCode(CodeFile codeFile);
}