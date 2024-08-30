# Markdown Code Aggregator

This application aggregates and analyzes code files within a given .NET solution directory. It compiles all relevant file content into a single Markdown file, optimized for input to various AI tools, enhancing capabilities such as code analysis, review, or transformation.

## Requirements

- Aggregate contents from all files within the solution into a single Markdown file.
- Provide an approximate token count and a summary of the files incorporated.
- Allow users to specify an exclusion file to omit certain files from aggregation.
- If no exclusion file is specified, use the .gitignore file at the root of the solution path as the default exclusion criterion.

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
3. Choose whether to use the default output directory or specify a custom one.
4. The application will generate a Markdown file with the aggregated code and a summary.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License.