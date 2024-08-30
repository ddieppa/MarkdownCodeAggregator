using CliWrap;
using CliWrap.Buffered;
using MarkdownCodeAggregator.Domain.Interfaces;
using Serilog;

namespace MarkdownCodeAggregator.Infrastructure.FileSystem;

public class GitBasedFileFilter(ILogger logger, IFileSystem fileSystem) : IFileFilter
{
    public async Task<IEnumerable<string>> GetTrackedFilesAsync(string directory)
    {
        try
        {
            var result = await Cli.Wrap("git")
                .WithArguments("ls-files --full-name")
                .WithWorkingDirectory(directory)
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync();

            if (result.ExitCode == 0)
            {
                var files = result.StandardOutput.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                return files.Select(file => Path.Combine(directory, file.Trim()));
            }
            else
            {
                logger.Warning("Git command failed. Falling back to directory scanning.");
                return FallbackGetFiles(directory);
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error executing git ls-files command. Falling back to directory scanning.");
            return FallbackGetFiles(directory);
        }
    }

    public async Task<bool> ShouldIncludeFileAsync(string filePath, string baseDirectory)
    {
        try
        {
            var relativePath = Path.GetRelativePath(baseDirectory, filePath).Replace('\\', '/');
            var result = await Cli.Wrap("git")
                .WithArguments($"check-ignore -q \"{relativePath}\"")
                .WithWorkingDirectory(baseDirectory)
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync();

            // If the exit code is 0, the file is ignored
            var isIgnored = result.ExitCode == 0;
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
        var filename = Path.GetFileName(filePath);
        var defaultIgnored = new[] { ".git", ".vs", "bin", "obj" };
        return defaultIgnored.Any(ignored => filePath.Contains(ignored, StringComparison.OrdinalIgnoreCase));
    }
}