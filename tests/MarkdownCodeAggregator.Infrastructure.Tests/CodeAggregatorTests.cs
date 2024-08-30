using FluentAssertions;
using MarkdownCodeAggregator.Domain.Interfaces;
using MarkdownCodeAggregator.Infrastructure.CodeAggregation;
using NSubstitute;
using Serilog;

namespace MarkdownCodeAggregator.Infrastructure.Tests;

public class CodeAggregatorTests
{
    private readonly IFileSystem _fileSystemMock;
    private readonly IFormatter _formatterMock;
    private readonly ITokenCounter _tokenCounterMock;
    private readonly IFileFilter _fileFilterMock;
    private readonly CodeAggregator _codeAggregator;

    public CodeAggregatorTests()
    {
        _fileSystemMock = Substitute.For<IFileSystem>();
        _formatterMock = Substitute.For<IFormatter>();
        _tokenCounterMock = Substitute.For<ITokenCounter>();
        _fileFilterMock = Substitute.For<IFileFilter>();
        var loggerMock = Substitute.For<ILogger>();

        _codeAggregator = new CodeAggregator(
            _fileSystemMock,
            _formatterMock,
            _tokenCounterMock,
            _fileFilterMock,
            loggerMock
        );
    }

    [Fact]
    public async Task AggregateCodeAsync_ShouldAggregateFilesCorrectly()
    {
        // Arrange
        var sourceDirectory = "/source";
        var outputDirectory = "/output";
        var files = new[] { "/source/file1.cs", "/source/file2.cs" };

        _fileFilterMock.GetTrackedFilesAsync(sourceDirectory).Returns(files);
        _fileFilterMock.ShouldIncludeFileAsync(Arg.Any<string>(), sourceDirectory).Returns(true);
        _fileSystemMock.ReadAllTextAsync(Arg.Any<string>()).Returns("file content");
        _formatterMock.FormatCode(Arg.Any<Domain.Entities.CodeFile>()).Returns("formatted content");
        _tokenCounterMock.CountTokens(Arg.Any<string>()).Returns(10);

        // Act
        var result = await _codeAggregator.AggregateCodeAsync(sourceDirectory, outputDirectory);

        // Assert
        result.FileCount.Should().Be(2);
        result.TotalTokens.Should().Be(20);
        result.Content.Should().Contain("formatted content");
    }
}