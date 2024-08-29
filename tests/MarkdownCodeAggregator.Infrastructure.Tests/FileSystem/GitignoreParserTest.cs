using FluentAssertions;
using NSubstitute;
using MarkdownCodeAggregator.Infrastructure.FileSystem;
using MarkdownCodeAggregator.Domain.Interfaces;
using Serilog;

namespace MarkdownCodeAggregator.Infrastructure.Tests.FileSystem;

public class GitignoreParserTest
{
    private readonly GitignoreParser _gitignoreParser;
    private readonly IFileSystem _fileSystemMock;

    public GitignoreParserTest()
    {
        _fileSystemMock = Substitute.For<IFileSystem>();
        _fileSystemMock.ReadAllTextAsync(Arg.Any<string>()).Returns(Task.FromResult(string.Empty));
        _fileSystemMock.FileExists(Arg.Any<string>()).Returns(true);
        _gitignoreParser = new GitignoreParser(_fileSystemMock, Substitute.For<ILogger>());
    }

    [Fact]
    public async Task ParseAsync_ReturnsEmptyArray_IfExcludeFilePathNullOrEmptyOrNonExistent()
    {
        var excludeFilePaths = new List<string> { null, string.Empty, "NonExistentPath" };
        _fileSystemMock.FileExists(Arg.Any<string>()).Returns(false);
        foreach (var path in excludeFilePaths)
        {
            var result = await _gitignoreParser.ParseAsync(path);
            result.Should().BeEmpty();
        }
    }

    [Fact]
    public async Task ParseAsync_ReturnsNonEmptyArray_IfExcludeFilePathIsValid()
    {
        var validFilePath = "ValidPath";
        var fileContents =
            @"# comment
            bin/
            obj/
            /packages/
            riderModule.iml
            /_ReSharper.Caches/";
        _fileSystemMock.ReadAllTextAsync(validFilePath).Returns(Task.FromResult(fileContents));
        var expectedResult = new List<string>
        {
            "bin/",
            "obj/",
            "/packages/",
            "riderModule.iml",
            "/_ReSharper.Caches/"
        };
        var result = (await _gitignoreParser.ParseAsync(validFilePath)).ToArray();
        result.Should().HaveCount(expectedResult.Count);
        result.Should().BeEquivalentTo(expectedResult);
    }

    [Theory]
    [InlineData("file.txt", "*.txt", true)]
    [InlineData("path/to/file.txt", "*.txt", true)]
    [InlineData("file.cs", "*.txt", false)]
    [InlineData("bin/debug/file.dll", "bin/", true)]
    [InlineData("obj/release/file.obj", "obj/", true)]
    [InlineData("src/file.cs", "bin/", false)]
    [InlineData("test/bin/debug/file.dll", "/bin/", false)]
    [InlineData("packages/newtonsoft.json/lib/net45/Newtonsoft.Json.dll", "/packages/", true)]
    [InlineData(".vs/ProjectSettings.json", ".vs/", true)]
    [InlineData("src/.vs/ProjectSettings.json", ".vs/", true)]
    [InlineData("README.md", "*.md", true)]
    [InlineData("docs/README.md", "/*.md", false)]
    [InlineData("lib/file.txt", "*.txt", true)]
    [InlineData(".vscode/settings.json", ".vscode/*", true)]
    [InlineData(".vscode/tasks.json", ".vscode/*", true)]
    [InlineData(".vscode/extensions.json", ".vscode/*", true)]
    [InlineData(".history/somefile", ".history/", true)]
    [InlineData("myextension.vsix", "*.vsix", true)]
    [InlineData(".idea/workspace.xml", ".idea/**/workspace.xml", true)]
    [InlineData("project/.idea/workspace.xml", ".idea/**/workspace.xml", true)]
    [InlineData(".idea/tasks.xml", ".idea/**/tasks.xml", true)]
    [InlineData("cmake-build-debug/", "cmake-build-*/", true)]
    [InlineData("file.iws", "*.iws", true)]
    [InlineData("out/production/myproject", "out/", true)]
    [InlineData(".idea_modules/", ".idea_modules/", true)]
    [InlineData("atlassian-ide-plugin.xml", "atlassian-ide-plugin.xml", true)]
    [InlineData(".idea/sonarlint/", ".idea/sonarlint/", true)]
    [InlineData("crashlytics-build.properties", "crashlytics-build.properties", true)]
    [InlineData(".idea/httpRequests/", ".idea/httpRequests/", true)]
    [InlineData(".idea/caches/build_file_checksums.ser", ".idea/caches/build_file_checksums.ser", true)]
    [InlineData(".fuse_hidden123456", ".fuse_hidden*", true)]
    [InlineData(".directory", ".directory", true)]
    [InlineData(".Trash-1000", ".Trash-*", true)]
    [InlineData(".nfs123456789", ".nfs*", true)]
    [InlineData("myfile.rsuser", "*.rsuser", true)]
    [InlineData("project.suo", "*.suo", true)]
    [InlineData("user.user", "*.user", true)]
    [InlineData(".userosscache", "*.userosscache", true)]
    [InlineData("project.sln.docstates", "*.sln.docstates", true)]
    [InlineData("mono_crash.mem.4592.1.blob", "mono_crash.*", true)]
    [InlineData("Debug/myfile.txt", "[Dd]ebug/", true)]
    [InlineData("Release/myfile.txt", "[Rr]elease/", true)]
    [InlineData("x64/myfile.txt", "x64/", true)]
    [InlineData("x86/myfile.txt", "x86/", true)]
    [InlineData("Win32/myfile.txt", "[Ww][Ii][Nn]32/", true)]
    [InlineData("ARM/myfile.txt", "[Aa][Rr][Mm]/", true)]
    [InlineData("ARM64/myfile.txt", "[Aa][Rr][Mm]64/", true)]
    [InlineData("bld/myfile.txt", "bld/", true)]
    [InlineData("Bin/myfile.txt", "[Bb]in/", true)]
    [InlineData("Obj/myfile.txt", "[Oo]bj/", true)]
    [InlineData("Log/myfile.txt", "[Ll]og/", true)]
    [InlineData("Logs/myfile.txt", "[Ll]ogs/", true)]
    public void ShouldIgnore_VariousScenarios(string path, string pattern, bool expectedResult)
    {
        var result = _gitignoreParser.ShouldIgnore(path, new[] { pattern });
        result.Should().Be(expectedResult);
    }

    [Fact]
    public void ShouldIgnore_ComplexPatterns()
    {
        var patterns = new[]
        {
            "*.swp",
            "*.bak",
            "*~",
            ".vs/",
            "bin/",
            "obj/",
            "/packages/",
            "!important.txt",
            ".vscode/*",
            "!.vscode/settings.json",
            "!.vscode/tasks.json",
            "!.vscode/launch.json",
            "!.vscode/extensions.json",
            "!.vscode/*.code-snippets",
            ".history/",
            "*.vsix",
            ".idea/**/workspace.xml",
            ".idea/**/tasks.xml",
            ".idea/**/usage.statistics.xml",
            ".idea/**/dictionaries",
            ".idea/**/shelf",
            ".idea/**/aws.xml",
            ".idea/**/contentModel.xml",
            "cmake-build-*/",
            "*.iws",
            "out/",
            ".idea_modules/",
            "atlassian-ide-plugin.xml",
            ".idea/replstate.xml",
            ".idea/sonarlint/",
            "com_crashlytics_export_strings.xml",
            "crashlytics.properties",
            "crashlytics-build.properties",
            "fabric.properties",
            ".idea/httpRequests/",
            ".idea/caches/build_file_checksums.ser"
        };

        _gitignoreParser.ShouldIgnore("file.swp", patterns).Should().BeTrue();
        _gitignoreParser.ShouldIgnore("backup.bak", patterns).Should().BeTrue();
        _gitignoreParser.ShouldIgnore("temp~", patterns).Should().BeTrue();
        _gitignoreParser.ShouldIgnore(".vs/settings.json", patterns).Should().BeTrue();
        _gitignoreParser.ShouldIgnore("project/bin/debug/app.exe", patterns).Should().BeTrue();
        _gitignoreParser.ShouldIgnore("project/obj/release/app.pdb", patterns).Should().BeTrue();
        _gitignoreParser.ShouldIgnore("packages/newtonsoft.json/lib/net45/Newtonsoft.Json.dll", patterns).Should().BeTrue();
        _gitignoreParser.ShouldIgnore("src/important.txt", patterns).Should().BeFalse();
        _gitignoreParser.ShouldIgnore("src/file.cs", patterns).Should().BeFalse();
        _gitignoreParser.ShouldIgnore(".vscode/settings.json", patterns).Should().BeFalse();
        _gitignoreParser.ShouldIgnore(".vscode/tasks.json", patterns).Should().BeFalse();
        _gitignoreParser.ShouldIgnore(".vscode/launch.json", patterns).Should().BeFalse();
        _gitignoreParser.ShouldIgnore(".vscode/extensions.json", patterns).Should().BeFalse();
        _gitignoreParser.ShouldIgnore(".vscode/my-snippet.code-snippets", patterns).Should().BeFalse();
        _gitignoreParser.ShouldIgnore(".history/some_file", patterns).Should().BeTrue();
        _gitignoreParser.ShouldIgnore("my_extension.vsix", patterns).Should().BeTrue();
        _gitignoreParser.ShouldIgnore(".idea/workspace.xml", patterns).Should().BeTrue();
        _gitignoreParser.ShouldIgnore("project/.idea/tasks.xml", patterns).Should().BeTrue();
        _gitignoreParser.ShouldIgnore(".idea/usage.statistics.xml", patterns).Should().BeTrue();
        _gitignoreParser.ShouldIgnore(".idea/dictionaries/my_dict", patterns).Should().BeTrue();
        _gitignoreParser.ShouldIgnore(".idea/shelf/something", patterns).Should().BeTrue();
        _gitignoreParser.ShouldIgnore("cmake-build-debug/", patterns).Should().BeTrue();
        _gitignoreParser.ShouldIgnore("project.iws", patterns).Should().BeTrue();
        _gitignoreParser.ShouldIgnore("out/production/myproject", patterns).Should().BeTrue();
        _gitignoreParser.ShouldIgnore(".idea_modules/mymodule", patterns).Should().BeTrue();
        _gitignoreParser.ShouldIgnore("atlassian-ide-plugin.xml", patterns).Should().BeTrue();
        _gitignoreParser.ShouldIgnore(".idea/replstate.xml", patterns).Should().BeTrue();
        _gitignoreParser.ShouldIgnore(".idea/sonarlint/someconfig", patterns).Should().BeTrue();
        _gitignoreParser.ShouldIgnore("com_crashlytics_export_strings.xml", patterns).Should().BeTrue();
        _gitignoreParser.ShouldIgnore("crashlytics.properties", patterns).Should().BeTrue();
        _gitignoreParser.ShouldIgnore("crashlytics-build.properties", patterns).Should().BeTrue();
        _gitignoreParser.ShouldIgnore("fabric.properties", patterns).Should().BeTrue();
        _gitignoreParser.ShouldIgnore(".idea/httpRequests/", patterns).Should().BeTrue();
        _gitignoreParser.ShouldIgnore(".idea/caches/build_file_checksums.ser", patterns).Should().BeTrue();
    }
}