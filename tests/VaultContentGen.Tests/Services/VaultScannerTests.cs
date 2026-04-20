using VaultContentGen.Config;
using VaultContentGen.Models;
using VaultContentGen.Services;

namespace VaultContentGen.Tests.Services;

public class VaultScannerTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    private void CreateFile(string relativePath, string content = "")
    {
        var fullPath = Path.Combine(_tempDir, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
    }

    private VaultScanner CreateScanner(
        List<string>? ignoredFolders = null,
        Dictionary<string, string>? sectionTypes = null) =>
        new(new AppConfig
        {
            VaultSourcePath = _tempDir,
            IgnoredFolders = ignoredFolders ?? [],
            SectionTypes = sectionTypes ?? []
        });

    [Fact]
    public void Scan_RootIndexMd_ParsedAsRootIndex()
    {
        CreateFile("Index.md", "# Hello");
        var result = CreateScanner().Scan();
        Assert.NotNull(result.RootIndex);
        Assert.Equal("Index.md", result.RootIndex.FileName);
    }

    [Fact]
    public void Scan_RootIndexMd_NotIncludedInStandaloneFiles()
    {
        CreateFile("Index.md", "# Hello");
        var result = CreateScanner().Scan();
        Assert.DoesNotContain(result.StandaloneFiles, f => f.FileName == "Index.md");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void Scan_RootMdFiles_ParsedAsStandaloneFiles()
    {
        CreateFile("Now.md");
        CreateFile("About.md");
        var result = CreateScanner().Scan();
        Assert.Equal(2, result.StandaloneFiles.Count);
    }

    [Fact]
    public void Scan_IgnoredFolders_AreSkipped()
    {
        CreateFile("TEMPCLEANUP/some-post.md");
        var result = CreateScanner(ignoredFolders: ["TEMPCLEANUP"]).Scan();
        Assert.Empty(result.Sections);
    }

    [Fact]
    public void Scan_Subfolders_ParsedAsSections()
    {
        CreateFile("Log/Index.md");
        var result = CreateScanner().Scan();
        Assert.Single(result.Sections);
        Assert.Equal("Log", result.Sections[0].Name);
    }

    [Fact]
    public void Scan_SectionIndexMd_SetAsSectionIndex()
    {
        CreateFile("Log/Index.md");
        CreateFile("Log/entry.md");
        var result = CreateScanner().Scan();
        Assert.NotNull(result.Sections[0].SectionIndex);
        Assert.Single(result.Sections[0].SectionFiles);
    }

    [Fact]
    public void Scan_NestedSubfolders_ParsedAsSubSections()
    {
        CreateFile("Library/Index.md");
        CreateFile("Library/Books/Index.md");
        var result = CreateScanner().Scan();
        Assert.Single(result.Sections[0].SubSections);
        Assert.Equal("Books", result.Sections[0].SubSections[0].Name);
    }

    [Fact]
    public void Scan_SectionTypes_AssignedCorrectly()
    {
        CreateFile("Library/Books/Index.md");
        var result = CreateScanner(
            sectionTypes: new Dictionary<string, string> { ["Library/Books"] = "Book" }
        ).Scan();
        Assert.Equal(ContentType.Book, result.Sections[0].SubSections[0].Type);
    }

    [Fact]
    public void Scan_YamlFrontmatter_ParsedIntoFrontMatter()
    {
        CreateFile("Now.md", """
        ---
        title: Now
        description: The now page
        ---
        Some body content
        """);
        var result = CreateScanner().Scan();
        Assert.Equal("Now", result.StandaloneFiles[0].FrontMatter["title"].ToString());
    }
}