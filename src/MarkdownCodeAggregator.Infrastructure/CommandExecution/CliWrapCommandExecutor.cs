using CliWrap;
using CliWrap.Buffered;
using MarkdownCodeAggregator.Domain.Interfaces;

namespace MarkdownCodeAggregator.Infrastructure.CommandExecution;

public class CliWrapCommandExecutor : ICommandExecutor
{
    public async Task<(string StandardOutput, string StandardError, int ExitCode)> ExecuteCommandAsync(
            string command,
            string arguments,
            string workingDirectory)
    {
        var result = await Cli.Wrap(command)
                             .WithArguments(arguments)
                             .WithWorkingDirectory(workingDirectory)
                             .WithValidation(CommandResultValidation.None)
                             .ExecuteBufferedAsync();

        return (result.StandardOutput, result.StandardError, result.ExitCode);
    }
}