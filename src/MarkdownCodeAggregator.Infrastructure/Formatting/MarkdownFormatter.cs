using MarkdownCodeAggregator.Domain.Entities;
using MarkdownCodeAggregator.Domain.Interfaces;

namespace MarkdownCodeAggregator.Infrastructure.Formatting;

public class MarkdownFormatter : IFormatter
{
    public string FormatCode(CodeFile codeFile)
    {
        var fileName = codeFile.Path.Value; // This is now the relative path
        var fileExtension = Path.GetExtension(fileName).TrimStart('.');
        return $"""
                ## File: {fileName}
                
                ```{fileExtension}
                {codeFile.Content}
                ```

                """;
    }
}