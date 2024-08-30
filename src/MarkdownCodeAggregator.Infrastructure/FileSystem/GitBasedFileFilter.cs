using MarkdownCodeAggregator.Domain.Interfaces;
using Serilog;
using System.Text.RegularExpressions;

namespace MarkdownCodeAggregator.Infrastructure.FileSystem;

public class GitBasedFileFilter(ILogger logger, IFileSystem fileSystem, ICommandExecutor commandExecutor)
        : IFileFilter
{
    public async Task<IEnumerable<string>> GetTrackedFilesAsync(string directory)
    {
        try
        {
            var (output, error, exitCode) = await commandExecutor.ExecuteCommandAsync(
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
                logger.Warning("Git command failed. Falling back to directory scanning. Error: {Error}", error);
                return FallbackGetFiles(directory);
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error executing git ls-files command. Falling back to directory scanning.");
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

            var (_, _, exitCode) = await commandExecutor.ExecuteCommandAsync(
                "git",
                $"check-ignore -q \"{relativePath}\"",
                baseDirectory);

            // If the exit code is 0, the file is ignored
            var isIgnored = exitCode == 0;
            logger.Debug("File {FilePath} is {IgnoreStatus} by Git", filePath, isIgnored ? "ignored" : "not ignored");
            return !isIgnored;
        }
        catch (Exception ex)
        {
            logger.Warning(ex, "Error checking if file {FilePath} should be ignored. Including file by default.", filePath);
            return true; // If there's an error, we'll include the file to be safe
        }
    }

    private IEnumerable<string> FallbackGetFiles(string directory)
    {
        return fileSystem.GetFiles(directory, "*.*", SearchOption.AllDirectories)
            .Where(file => !IsDefaultIgnoredFile(file));
    }

    private bool IsDefaultIgnoredFile(string filePath)
    {
        // var filename = Path.GetFileName(filePath);
        var defaultIgnored = new[] { ".git", ".vs", "bin", "obj" };
        return defaultIgnored.Any(ignored => filePath.Contains(ignored, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsExcludedByFile(string relativePath, string excludeFilePath)
    {
        var excludePatterns = File.ReadAllLines(excludeFilePath)
            .Where(line => !string.IsNullOrWhiteSpace(line) && !line.TrimStart().StartsWith("#"))
            .Select(line => WildcardToRegex(line.Trim()))
            .ToList();

        return excludePatterns.Any(pattern => Regex.IsMatch(relativePath, pattern, RegexOptions.IgnoreCase));
    }

    private string WildcardToRegex(string pattern)
    {
        return "^" + Regex.Escape(pattern)
                          .Replace("\\*", ".*")
                          .Replace("\\?", ".")
                   + "$";
    }
}