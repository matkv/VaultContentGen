using VaultContentGen.Config;
using VaultContentGen.Models;
using VaultContentGen.Services;

namespace VaultContentGen.Tests.Services;

public class HugoWriterTests : IDisposable
{
    private readonly string _vaultDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    private readonly string _hugoSiteDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    private string HugoContentDir => Path.Combine(_hugoSiteDir, "content");

    private HugoWriter CreateWriter() =>
        new(new AppConfig
        {
            VaultSourcePath = _vaultDir,
            HugoContentPath = HugoContentDir,
        });

    private ObsidianFile MakeFile(string name, string body, ContentType type = ContentType.Standard) =>
        new()
        {
            FileName = name,
            SourcePath = Path.Combine(_vaultDir, name),
            Body = body,
            FrontMatter = new Dictionary<string, object> { ["title"] = Path.GetFileNameWithoutExtension(name) },
            Type = type,
        };

    private void CreateVaultImage(string relativePath, byte[]? bytes = null)
    {
        var fullPath = Path.Combine(_vaultDir, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllBytes(fullPath, bytes ?? [0x89, 0x50, 0x4E, 0x47]);
    }

    public void Dispose()
    {
        if (Directory.Exists(_vaultDir)) Directory.Delete(_vaultDir, recursive: true);
        if (Directory.Exists(_hugoSiteDir)) Directory.Delete(_hugoSiteDir, recursive: true);
    }

    [Fact]
    public void ImageComment_CopiedToStaticAndReplacedWithMarkdown()
    {
        CreateVaultImage("Attachments/Ghostty.png");
        var structure = new ObsidianStructure
        {
            StandaloneFiles =
            [
                MakeFile("Post.md", "Hello\n<!-- Image: Attachments/Ghostty.png -->\nWorld"),
            ],
        };

        CreateWriter().Write(structure);

        var outputContent = File.ReadAllText(Path.Combine(HugoContentDir, "post.md"));
        Assert.Contains("![Ghostty](/images/standard/Ghostty.png)", outputContent);
        Assert.DoesNotContain("<!-- Image:", outputContent);

        var copiedImage = Path.Combine(_hugoSiteDir, "static", "images", "standard", "Ghostty.png");
        Assert.True(File.Exists(copiedImage));
    }

    [Fact]
    public void ImageComment_ContentTypeUsedAsSubfolder()
    {
        CreateVaultImage("Attachments/Ghostty.png");
        var structure = new ObsidianStructure
        {
            StandaloneFiles =
            [
                MakeFile("Entry.md", "<!-- Image: Attachments/Ghostty.png -->", ContentType.Log),
            ],
        };

        CreateWriter().Write(structure);

        var outputContent = File.ReadAllText(Path.Combine(HugoContentDir, "entry.md"));
        Assert.Contains("![Ghostty](/images/log/Ghostty.png)", outputContent);
        Assert.True(File.Exists(Path.Combine(_hugoSiteDir, "static", "images", "log", "Ghostty.png")));
    }

    [Fact]
    public void IndexFile_HasRootUrlInFrontmatter()
    {
        var structure = new ObsidianStructure
        {
            Sections =
            [
                new ObsidianSection
                {
                    Name = "Garden",
                    SourcePath = Path.Combine(_vaultDir, "Garden"),
                    Type = ContentType.Index,
                    SectionFiles = [MakeFile("My Entry.md", "body", ContentType.Index)],
                }
            ],
        };

        CreateWriter().Write(structure);

        var outputContent = File.ReadAllText(Path.Combine(HugoContentDir, "garden", "my-entry.md"));
        Assert.Contains("url = \"/my-entry\"", outputContent);
        Assert.Contains("index_entry = true", outputContent);
        Assert.Contains("section_path = \"/\"", outputContent);
    }

    [Fact]
    public void IndexSubsection_WrittenAtContentRoot()
    {
        var structure = new ObsidianStructure
        {
            Sections =
            [
                new ObsidianSection
                {
                    Name = "Garden",
                    SourcePath = Path.Combine(_vaultDir, "Garden"),
                    Type = ContentType.Index,
                    SectionFiles = [],
                    SubSections =
                    [
                        new ObsidianSection
                        {
                            Name = "Programming",
                            SourcePath = Path.Combine(_vaultDir, "Garden", "Programming"),
                            Type = ContentType.Standard,
                            SectionFiles = [MakeFile("My Entry.md", "body", ContentType.Standard)],
                            SubSections = [],
                        }
                    ],
                }
            ],
        };

        CreateWriter().Write(structure);

        Assert.True(File.Exists(Path.Combine(HugoContentDir, "programming", "my-entry.md")));
        Assert.False(File.Exists(Path.Combine(HugoContentDir, "index", "programming", "my-entry.md")));

        var outputContent = File.ReadAllText(Path.Combine(HugoContentDir, "programming", "my-entry.md"));
        Assert.DoesNotContain("url =", outputContent);
        Assert.Contains("index_entry = true", outputContent);
        Assert.Contains("section_path = \"/programming\"", outputContent);
    }

    [Fact]
    public void ImageComment_MissingSourceFile_CommentPreserved()
    {
        var structure = new ObsidianStructure
        {
            StandaloneFiles =
            [
                MakeFile("Post.md", "<!-- Image: Attachments/Missing.png -->"),
            ],
        };

        CreateWriter().Write(structure);

        var outputContent = File.ReadAllText(Path.Combine(HugoContentDir, "post.md"));
        Assert.Contains("<!-- Image: Attachments/Missing.png -->", outputContent);
    }
}
