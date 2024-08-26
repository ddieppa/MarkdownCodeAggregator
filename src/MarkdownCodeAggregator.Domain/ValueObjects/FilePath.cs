namespace MarkdownCodeAggregator.Domain.ValueObjects;

public class FilePath(string value)
{
    public string Value { get; } = string.IsNullOrWhiteSpace(value)
                                           ? throw new ArgumentException("File path cannot be empty", nameof(value))
                                           : value;

    public static implicit operator string(FilePath path) => path.Value;
    public static explicit operator FilePath(string path) => new(path);
}