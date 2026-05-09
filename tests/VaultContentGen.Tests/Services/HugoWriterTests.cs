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
