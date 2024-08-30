using MarkdownCodeAggregator.Domain.Interfaces;
using Serilog;
using System.Threading.Tasks;

namespace MarkdownCodeAggregator.Infrastructure.FileSystem;

public class GitBasedFileFilter : IFileFilter
{
    private readonly ILogger _logger;
    private readonly IFileSystem _fileSystem;
    private readonly ICommandExecutor _commandExecutor;

    public GitBasedFileFilter(ILogger logger, IFileSystem fileSystem, ICommandExecutor commandExecutor)
    {
        _logger = logger;
        _fileSystem = fileSystem;
        _commandExecutor = commandExecutor;
    }

    public async Task<IEnumerable<string>> GetTrackedFilesAsync(string directory)
    {
        try
        {
            var (output, error, exitCode) = await _commandExecutor.ExecuteCommandAsync(
                "git",
                "ls-files --full-name",
                directory);

            if (exitCode == 0)
            {
                var files = output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                return files.Select(file => Path.Combine(directory, file.Trim()));
            }
            else
            {
                _logger.Warning("Git command failed. Falling back to directory scanning. Error: {Error}", error);
                return FallbackGetFiles(directory);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error executing git ls-files command. Falling back to directory scanning.");
            return FallbackGetFiles(directory);
        }
    }

    public async Task<bool> ShouldIncludeFileAsync(string filePath, string baseDirectory, string? excludeFilePath)
    {
        try
        {
            var relativePath = Path.GetRelativePath(baseDirectory, filePath).Replace('\\', '/');

            // If an exclude file is specified, use it instead of Git's ignore rules
            if (!string.IsNullOrEmpty(excludeFilePath))
            {
                return !IsExcludedByFile(relativePath, excludeFilePath);
            }

            var (_, _, exitCode) = await _commandExecutor.ExecuteCommandAsync(
                "git",
                $"check-ignore -q \"{relativePath}\"",
                baseDirectory);

            // If the exit code is 0, the file is ignored
            var isIgnored = exitCode == 0;
            _logger.Debug("File {FilePath} is {IgnoreStatus} by Git", filePath, isIgnored ? "ignored" : "not ignored");
            return !isIgnored;
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Error checking if file {FilePath} should be ignored. Including file by default.", filePath);
            return true; // If there's an error, we'll include the file to be safe
        }
    }

    private IEnumerable<string> FallbackGetFiles(string directory)
    {
        return _fileSystem.GetFiles(directory, "*.*", SearchOption.AllDirectories)
            .Where(file => !IsDefaultIgnoredFile(file));
    }

    private bool IsDefaultIgnoredFile(string filePath)
    {
        var filename = Path.GetFileName(filePath);
        var defaultIgnored = new[] { ".git", ".vs", "bin", "obj" };
        return defaultIgnored.Any(ignored => filePath.Contains(ignored, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsExcludedByFile(string relativePath, string excludeFilePath)
    {
        var excludePatterns = File.ReadAllLines(excludeFilePath)
            .Where(line => !string.IsNullOrWhiteSpace(line) && !line.TrimStart().StartsWith("#"))
            .ToList();

        foreach (var pattern in excludePatterns)
        {
            if (IsMatch(relativePath, pattern))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsMatch(string path, string pattern)
    {
        // This is a simple implementation. You might want to use a more robust glob matching library.
        return path.Contains(pattern, StringComparison.OrdinalIgnoreCase);
    }
}