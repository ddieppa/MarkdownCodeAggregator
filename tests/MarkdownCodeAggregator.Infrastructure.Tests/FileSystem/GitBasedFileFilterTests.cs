using FluentAssertions;
using MarkdownCodeAggregator.Domain.Interfaces;
using MarkdownCodeAggregator.Infrastructure.FileSystem;
using NSubstitute;
using Serilog;

namespace MarkdownCodeAggregator.Infrastructure.Tests.FileSystem;

public class GitBasedFileFilterTests : IDisposable
{
    private readonly ILogger _loggerMock;
    private readonly IFileSystem _fileSystemMock;
    private readonly ICommandExecutor _commandExecutorMock;
    private readonly GitBasedFileFilter _gitBasedFileFilter;
    private readonly string _tempDirectory;

    public GitBasedFileFilterTests()
    {
        _loggerMock = Substitute.For<ILogger>();
        _fileSystemMock = Substitute.For<IFileSystem>();
        _commandExecutorMock = Substitute.For<ICommandExecutor>();
        _gitBasedFileFilter = new GitBasedFileFilter(_loggerMock, _fileSystemMock, _commandExecutorMock);

        _tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

    [Fact]
    public async Task GetTrackedFilesAsync_ShouldReturnFilesWhenGitCommandSucceeds()
    {
        // Arrange
        var expectedFiles = new[] { "file1.cs", "file2.cs" };
        _commandExecutorMock.ExecuteCommandAsync("git", "ls-files --full-name", _tempDirectory)
            .Returns(Task.FromResult((string.Join("\n", expectedFiles), "", 0)));

        // Act
        var result = await _gitBasedFileFilter.GetTrackedFilesAsync(_tempDirectory);

        // Assert
        result.Should().BeEquivalentTo(expectedFiles.Select(f => Path.Combine(_tempDirectory, f)));
    }

    [Fact]
    public async Task GetTrackedFilesAsync_ShouldFallbackToFileSystemWhenGitCommandFails()
    {
        // Arrange
        var expectedFiles = new[] { Path.Combine(_tempDirectory, "file1.cs"), Path.Combine(_tempDirectory, "file2.cs") };
        _commandExecutorMock.ExecuteCommandAsync("git", "ls-files --full-name", _tempDirectory)
            .Returns(Task.FromResult(("", "error", 1)));
        _fileSystemMock.GetFiles(_tempDirectory, "*.*", SearchOption.AllDirectories).Returns(expectedFiles);

        // Act
        var result = await _gitBasedFileFilter.GetTrackedFilesAsync(_tempDirectory);

        // Assert
        result.Should().BeEquivalentTo(expectedFiles);
    }

    [Fact]
    public async Task ShouldIncludeFileAsync_ShouldReturnTrueWhenFileIsNotIgnoredByGit()
    {
        // Arrange
        var filePath = Path.Combine(_tempDirectory, "file1.cs");
        _commandExecutorMock.ExecuteCommandAsync("git", $"check-ignore -q \"file1.cs\"", _tempDirectory)
            .Returns(Task.FromResult(("", "", 1)));

        // Act
        var result = await _gitBasedFileFilter.ShouldIncludeFileAsync(filePath, _tempDirectory, null);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldIncludeFileAsync_ShouldReturnFalseWhenFileIsIgnoredByGit()
    {
        // Arrange
        var filePath = Path.Combine(_tempDirectory, "file1.cs");
        _commandExecutorMock.ExecuteCommandAsync("git", $"check-ignore -q \"file1.cs\"", _tempDirectory)
            .Returns(Task.FromResult(("", "", 0)));

        // Act
        var result = await _gitBasedFileFilter.ShouldIncludeFileAsync(filePath, _tempDirectory, null);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldIncludeFileAsync_ShouldUseExcludeFileWhenProvided()
    {
        // Arrange
        var filePath = Path.Combine(_tempDirectory, "file1.cs");
        var excludeFilePath = Path.Combine(_tempDirectory, ".gitignore");

        File.WriteAllText(excludeFilePath, "*.cs");

        // Act
        var result = await _gitBasedFileFilter.ShouldIncludeFileAsync(filePath, _tempDirectory, excludeFilePath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldIncludeFileAsync_ShouldIncludeFileWhenNotMatchingExcludePattern()
    {
        // Arrange
        var filePath = Path.Combine(_tempDirectory, "file1.txt");
        var excludeFilePath = Path.Combine(_tempDirectory, ".gitignore");

        File.WriteAllText(excludeFilePath, "*.cs");

        // Act
        var result = await _gitBasedFileFilter.ShouldIncludeFileAsync(filePath, _tempDirectory, excludeFilePath);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldIncludeFileAsync_ShouldHandleMultipleExcludePatterns()
    {
        // Arrange
        var filePath1 = Path.Combine(_tempDirectory, "file1.cs");
        var filePath2 = Path.Combine(_tempDirectory, "file2.txt");
        var excludeFilePath = Path.Combine(_tempDirectory, ".gitignore");

        File.WriteAllText(excludeFilePath, "*.cs\n*.txt");

        // Act
        var result1 = await _gitBasedFileFilter.ShouldIncludeFileAsync(filePath1, _tempDirectory, excludeFilePath);
        var result2 = await _gitBasedFileFilter.ShouldIncludeFileAsync(filePath2, _tempDirectory, excludeFilePath);

        // Assert
        result1.Should().BeFalse();
        result2.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldIncludeFileAsync_ShouldIgnoreCommentsInExcludeFile()
    {
        // Arrange
        var filePath = Path.Combine(_tempDirectory, "file1.cs");
        var excludeFilePath = Path.Combine(_tempDirectory, ".gitignore");

        File.WriteAllText(excludeFilePath, "# This is a comment\n*.cs");

        // Act
        var result = await _gitBasedFileFilter.ShouldIncludeFileAsync(filePath, _tempDirectory, excludeFilePath);

        // Assert
        result.Should().BeFalse();
    }
}