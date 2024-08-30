using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spectre.Console;
using MarkdownCodeAggregator.Application.Interfaces;
using MarkdownCodeAggregator.Application.Services;
using MarkdownCodeAggregator.Domain.Interfaces;
using MarkdownCodeAggregator.Infrastructure.FileSystem;
using MarkdownCodeAggregator.Infrastructure.Formatting;
using MarkdownCodeAggregator.Infrastructure.CodeAggregation;
using MarkdownCodeAggregator.Infrastructure.TokenCounting;
using MarkdownCodeAggregator.Infrastructure.CommandExecution;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/markdown_aggregator_.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting Markdown Code Aggregator");

    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddSingleton<IFileSystem, FileSystemAdapter>();
    builder.Services.AddSingleton<IFormatter, MarkdownFormatter>();
    builder.Services.AddSingleton<ITokenCounter, AdvancedTokenCounter>();
    builder.Services.AddSingleton<IFileFilter, GitBasedFileFilter>();
    builder.Services.AddSingleton<ICodeAggregator, CodeAggregator>();
    builder.Services.AddSingleton<ICodeAggregatorService, CodeAggregatorService>();
    builder.Services.AddSingleton<ICommandExecutor, CliWrapCommandExecutor>();
    builder.Services.AddSingleton(Log.Logger);

    builder.Logging.AddSerilog();

    using var host = builder.Build();

    var aggregatorService = host.Services.GetRequiredService<ICodeAggregatorService>();
    var fileSystem = host.Services.GetRequiredService<IFileSystem>();

    while (true)
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new FigletText("Markdown Code Aggregator").Centered().Color(Color.Blue));

        var sourceDirectory = AnsiConsole.Ask<string>("Enter the [green]source directory[/] to aggregate:");

        if (!Directory.Exists(sourceDirectory))
        {
            AnsiConsole.MarkupLine("[red]Error:[/] The specified source directory does not exist.");
            if (!AnsiConsole.Confirm("Do you want to try again?"))
                break;
            continue;
        }

        string? excludeFilePath = null;
        var gitignorePath = Path.Combine(sourceDirectory, ".gitignore");
        if (File.Exists(gitignorePath))
        {
            if (AnsiConsole.Confirm($"A [cyan].gitignore[/] file was found. Do you want to use it for exclusions?"))
            {
                excludeFilePath = gitignorePath;
            }
        }

        if (excludeFilePath == null)
        {
            if (AnsiConsole.Confirm("Do you want to specify an exclude file?"))
            {
                excludeFilePath = AnsiConsole.Ask<string>("Enter the path to the [green]exclude file[/]:");
                if (!File.Exists(excludeFilePath))
                {
                    AnsiConsole.MarkupLine($"[yellow]Warning:[/] The specified exclude file does not exist. Proceeding without exclusions.");
                    excludeFilePath = null;
                }
            }
        }

        string outputDirectory;
        var defaultOutputDirectory = Path.Combine(sourceDirectory, "aggregated-code");
        if (AnsiConsole.Confirm($"Do you want to use the default output directory? ([green]{defaultOutputDirectory}[/])"))
        {
            outputDirectory = defaultOutputDirectory;
            if (Directory.Exists(outputDirectory))
            {
                if (AnsiConsole.Confirm("The output directory already exists. Do you want to clean it first?"))
                {
                    Directory.Delete(outputDirectory, true);
                    Directory.CreateDirectory(outputDirectory);
                }
            }
            else
            {
                Directory.CreateDirectory(outputDirectory);
            }
        }
        else
        {
            outputDirectory = AnsiConsole.Ask<string>("Enter the path for the [green]output directory[/]:");
            Directory.CreateDirectory(outputDirectory);
        }

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var outputFile = Path.Combine(outputDirectory, $"{timestamp}.md");

        try
        {
            var result = await AnsiConsole.Progress()
                .StartAsync(async ctx =>
                {
                    var task = ctx.AddTask("Aggregating files");
                    return await aggregatorService.AggregateCodeAsync(sourceDirectory, outputDirectory, excludeFilePath, (fileName, progress) =>
                    {
                        task.Description = $"Aggregating: {fileName}";
                        task.Value = progress * 100;
                    });
                });

            await fileSystem.WriteAllTextAsync(outputFile, result.AggregatedContent);

            AnsiConsole.MarkupLine("[green]Aggregation complete![/]");
            AnsiConsole.MarkupLine($"Total files processed: [blue]{result.FileCount}[/]");
            AnsiConsole.MarkupLine($"Total tokens: [blue]{result.TokenCount}[/]");
            AnsiConsole.MarkupLine($"Output file: [blue]{outputFile}[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]An error occurred during aggregation:[/] {ex.Message}");
            Log.Error(ex, "An error occurred during aggregation");
        }

        if (!AnsiConsole.Confirm("Do you want to aggregate another directory?"))
            break;
    }

    AnsiConsole.MarkupLine("[green]Thank you for using Markdown Code Aggregator. Goodbye![/]");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    AnsiConsole.WriteException(ex);
}
finally
{
    Log.CloseAndFlush();
}