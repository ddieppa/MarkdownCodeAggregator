using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spectre.Console;
using MarkdownCodeAggregator.Application.Interfaces;
using MarkdownCodeAggregator.Application.Services;
using MarkdownCodeAggregator.Domain.Interfaces;
using MarkdownCodeAggregator.Infrastructure.FileSystem;
using MarkdownCodeAggregator.Infrastructure.Formatting;
using MarkdownCodeAggregator.Infrastructure.TokenCounting;
using MarkdownCodeAggregator.Infrastructure.CodeAggregation;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<IFileSystem, FileSystemAdapter>();
builder.Services.AddSingleton<IFormatter, MarkdownFormatter>();
builder.Services.AddSingleton<ITokenCounter, SimpleTokenCounter>();
builder.Services.AddSingleton<ICodeAggregator, CodeAggregator>();
builder.Services.AddSingleton<ICodeAggregatorService, CodeAggregatorService>();

using var host = builder.Build();

var aggregatorService = host.Services.GetRequiredService<ICodeAggregatorService>();
var fileSystem = host.Services.GetRequiredService<IFileSystem>();

AnsiConsole.Write(new FigletText("Markdown Code Aggregator").Centered().Color(Color.Blue));

var sourceDirectory = AnsiConsole.Ask<string>("Enter the [green]source directory[/] to aggregate:");
if (!Directory.Exists(sourceDirectory))
{
    AnsiConsole.MarkupLine($"[red]Error:[/] The specified source directory does not exist.");
    return;
}

string? excludeFile = null;
if (AnsiConsole.Confirm("Do you want to specify an exclude file?"))
{
    excludeFile = AnsiConsole.Ask<string>("Enter the path to the [green]exclude file[/]:");
    if (!File.Exists(excludeFile))
    {
        AnsiConsole.MarkupLine($"[yellow]Warning:[/] The specified exclude file does not exist. Proceeding without exclusions.");
        excludeFile = null;
    }
}

var outputFile = AnsiConsole.Ask<string>("Enter the path for the [green]output file[/]:");
if (!Path.HasExtension(outputFile))
{
    outputFile += ".md";
}

var outputDirectory = Path.GetDirectoryName(outputFile);
if (!string.IsNullOrEmpty(outputDirectory) && !Directory.Exists(outputDirectory))
{
    AnsiConsole.MarkupLine($"[yellow]Warning:[/] The output directory does not exist. Attempting to create it.");
    try
    {
        Directory.CreateDirectory(outputDirectory);
    }
    catch (Exception ex)
    {
        AnsiConsole.MarkupLine($"[red]Error:[/] Failed to create the output directory: {ex.Message}");
        return;
    }
}

try
{
    var result = await AnsiConsole.Progress()
        .StartAsync(async ctx =>
        {
            var task = ctx.AddTask("Aggregating files");
            return await aggregatorService.AggregateCodeAsync(sourceDirectory, excludeFile, (fileName, progress) =>
            {
                task.Description = $"Aggregating: {fileName}";
                task.Value = progress * 100;
            });
        });

    await fileSystem.WriteAllTextAsync(outputFile, result.AggregatedContent);

    AnsiConsole.MarkupLine($"[green]Aggregation complete![/]");
    AnsiConsole.MarkupLine($"Total files processed: [blue]{result.FileCount}[/]");
    AnsiConsole.MarkupLine($"Approximate token count: [blue]{result.TokenCount}[/]");
    AnsiConsole.MarkupLine($"Output file: [blue]{outputFile}[/]");
}
catch (UnauthorizedAccessException ex)
{
    AnsiConsole.MarkupLine($"[red]Error:[/] You don't have permission to write to the output file. Try running the application as an administrator or choose a different location.");
    AnsiConsole.MarkupLine($"Details: {ex.Message}");
}
catch (IOException ex)
{
    AnsiConsole.MarkupLine($"[red]Error:[/] An I/O error occurred. The file might be in use by another process or you might not have the necessary permissions.");
    AnsiConsole.MarkupLine($"Details: {ex.Message}");
}
catch (Exception ex)
{
    AnsiConsole.MarkupLine($"[red]An unexpected error occurred:[/] {ex.Message}");
    AnsiConsole.MarkupLine("Stack trace:");
    AnsiConsole.WriteException(ex);
}