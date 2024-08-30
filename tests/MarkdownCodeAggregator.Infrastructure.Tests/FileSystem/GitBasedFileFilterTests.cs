using FluentAssertions;
using MarkdownCodeAggregator.Domain.Interfaces;
using MarkdownCodeAggregator.Infrastructure.FileSystem;
using NSubstitute;
using Serilog;

namespace MarkdownCodeAggregator.Infrastructure.Tests.FileSystem;

public class GitBasedFileFilterTests
{
    private readonly IFileSystem _fileSystemMock;
    private readonly ICommandExecutor _commandExecutorMock;
    private readonly GitBasedFileFilter _gitBasedFileFilter;

    public GitBasedFileFilterTests()
    {
        var loggerMock = Substitute.For<ILogger>();
        _fileSystemMock = Substitute.For<IFileSystem>();
        _commandExecutorMock = Substitute.For<ICommandExecutor>();
        _gitBasedFileFilter = new GitBasedFileFilter(loggerMock, _fileSystemMock, _commandExecutorMock);
    }

    [Fact]
    public async Task GetTrackedFilesAsync_ShouldReturnFilesWhenGitCommandSucceeds()
    {
        // Arrange
        var directory = "/test";
        var expectedFiles = new[] { "file1.cs", "file2.cs" };
        _commandExecutorMock.ExecuteCommandAsync("git", "ls-files --full-name", directory)
                .Returns(Task.FromResult((string.Join("\n", expectedFiles), "", 0)));

        // Act
        var result = await _gitBasedFileFilter.GetTrackedFilesAsync(directory);

        // Assert
        result.Should().BeEquivalentTo(expectedFiles.Select(f => Path.Combine(directory, f)));
    }

    [Fact]
    public async Task GetTrackedFilesAsync_ShouldFallbackToFileSystemWhenGitCommandFails()
    {
        // Arrange
        var directory = "/test";
        var expectedFiles = new[] { "/test/file1.cs", "/test/file2.cs" };
        _commandExecutorMock.ExecuteCommandAsync("git", "ls-files --full-name", directory)
                .Returns(Task.FromResult(("", "error", 1)));
        _fileSystemMock.GetFiles(directory, "*.*", SearchOption.AllDirectories).Returns(expectedFiles);

        // Act
        var result = await _gitBasedFileFilter.GetTrackedFilesAsync(directory);

        // Assert
        result.Should().BeEquivalentTo(expectedFiles);
    }

    [Fact]
    public async Task ShouldIncludeFileAsync_ShouldReturnTrueWhenFileIsNotIgnored()
    {
        // Arrange
        var filePath = "/test/file1.cs";
        var baseDirectory = "/test";
        _commandExecutorMock.ExecuteCommandAsync("git", $"check-ignore -q \"file1.cs\"", baseDirectory)
                .Returns(Task.FromResult(("", "", 1)));

        // Act
        var result = await _gitBasedFileFilter.ShouldIncludeFileAsync(filePath, baseDirectory);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldIncludeFileAsync_ShouldReturnFalseWhenFileIsIgnored()
    {
        // Arrange
        var filePath = "/test/file1.cs";
        var baseDirectory = "/test";
        _commandExecutorMock.ExecuteCommandAsync("git", $"check-ignore -q \"file1.cs\"", baseDirectory)
                .Returns(Task.FromResult(("", "", 0)));

        // Act
        var result = await _gitBasedFileFilter.ShouldIncludeFileAsync(filePath, baseDirectory);

        // Assert
        result.Should().BeFalse();
    }
}