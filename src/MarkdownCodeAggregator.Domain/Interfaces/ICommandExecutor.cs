namespace MarkdownCodeAggregator.Domain.Interfaces;

public interface ICommandExecutor
{
    Task<(string StandardOutput, string StandardError, int ExitCode)> ExecuteCommandAsync(
            string command,
            string arguments,
            string workingDirectory);
}