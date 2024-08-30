# Markdown Code Aggregator

This application aggregates and analyzes code files within a given .NET solution directory. It compiles all relevant file content into a single Markdown file, optimized for input to various AI tools, enhancing capabilities such as code analysis, review, or transformation.

## Features

- Aggregates contents from all files within the solution into a single Markdown file.
- Provides an approximate token count and a summary of the files incorporated.
- Allows users to specify an exclusion file to omit certain files from aggregation.
- Automatically detects and offers to use .gitignore file for exclusions.
- Flexible output options, including using a default directory or specifying a custom one.

## Technology Stack

- .NET Core 8
- C# 12 (latest language features)
- Spectre.Console for enhanced console-based interaction
- CliWrap for executing Git commands
- Clean architecture pattern
- xUnit, FluentAssertions, and NSubstitute for testing

## Project Structure

1. MarkdownCodeAggregator.Domain
   - Contains core business logic and interfaces

2. MarkdownCodeAggregator.Application
   - Contains application services and DTOs

3. MarkdownCodeAggregator.Infrastructure
   - Contains implementations of interfaces defined in the Domain layer

4. MarkdownCodeAggregator.Console
   - Contains the console application entry point and user interface logic

5. MarkdownCodeAggregator.Tests
   - Contains unit tests for the application

## Usage

1. Run the console application.
2. Enter the source directory path when prompted.
3. If a .gitignore file is detected, you'll be asked if you want to use it for exclusions.
   - If you choose not to use .gitignore, you'll be prompted to specify an alternative exclude file.
4. Choose whether to use the default output directory (aggregated-code) or specify a custom one.
   - If the default directory already exists, you'll be asked if you want to clean it first.
5. The application will generate a Markdown file with the aggregated code and a summary.
6. You'll be given the option to aggregate another directory or exit the application.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License.