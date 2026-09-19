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
            MovieSourcePath = Path.Combine(_vaultDir, "Movies"),
        });

    private ObsidianFile MakeMovie(string name, Dictionary<string, object> frontMatter) =>
        new()
        {
            FileName = name,
            SourcePath = Path.Combine(_vaultDir, "Movies", name),
            Body = string.Empty,
            FrontMatter = frontMatter,
            Type = ContentType.Movie,
        };

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

    [Fact]
    public void ImageComment_FilenameWithSpaces_UrlEncodedInOutput()
    {
        CreateVaultImage("Attachments/Symphony Series Collection.png");
        var structure = new ObsidianStructure
        {
            StandaloneFiles =
            [
                MakeFile("Post.md", "<!-- Image: Attachments/Symphony Series Collection.png -->", ContentType.Log),
            ],
        };

        CreateWriter().Write(structure);

        var outputContent = File.ReadAllText(Path.Combine(HugoContentDir, "post.md"));
        Assert.Contains("![Symphony Series Collection](/images/log/Symphony%20Series%20Collection.png)", outputContent);
        Assert.True(File.Exists(Path.Combine(_hugoSiteDir, "static", "images", "log", "Symphony Series Collection.png")));
    }

    [Fact]
    public void Movie_WrittenWithFrontmatterAndCover()
    {
        CreateVaultImage("Movies/Covers/dune-part-two-2024.jpg");
        var structure = new ObsidianStructure
        {
            Movies =
            [
                MakeMovie("Dune Part Two (2024).md", new Dictionary<string, object>
                {
                    ["title"] = "Dune: Part Two",
                    ["type"] = "Movie",
                    ["year"] = "2024",
                    ["rating"] = "8",
                    ["date"] = "2024-03-09",
                    ["cover"] = "Covers/dune-part-two-2024.jpg",
                }),
            ],
        };

        CreateWriter().Write(structure);

        var outputContent = File.ReadAllText(
            Path.Combine(HugoContentDir, "library", "movies-tv-shows", "dune-part-two-(2024).md"));
        Assert.Contains("title = \"Dune: Part Two\"", outputContent);
        Assert.Contains("date = \"2024-03-09\"", outputContent);
        Assert.Contains("media_type = \"Movie\"", outputContent);
        Assert.DoesNotContain("\ntype =", outputContent);
        Assert.Contains("year = \"2024\"", outputContent);
        Assert.Contains("rating = \"8\"", outputContent);
        Assert.Contains("cover = \"/covers/movies-tv-shows/dune-part-two-2024.jpg\"", outputContent);
        Assert.True(File.Exists(
            Path.Combine(_hugoSiteDir, "static", "covers", "movies-tv-shows", "dune-part-two-2024.jpg")));
    }

    [Fact]
    public void Movie_WithoutDate_HasNoDateField()
    {
        var structure = new ObsidianStructure
        {
            Movies =
            [
                MakeMovie("Breaking Bad (2008).md", new Dictionary<string, object>
                {
                    ["title"] = "Breaking Bad",
                    ["type"] = "TV Show",
                    ["date"] = null!,
                }),
            ],
        };

        CreateWriter().Write(structure);

        var outputContent = File.ReadAllText(
            Path.Combine(HugoContentDir, "library", "movies-tv-shows", "breaking-bad-(2008).md"));
        Assert.DoesNotContain("date =", outputContent);
        Assert.Contains("media_type = \"TV Show\"", outputContent);
    }

    [Fact]
    public void BookAndMovie_HaveBuildRenderNever()
    {
        var structure = new ObsidianStructure
        {
            Books = [MakeFile("Some Book.md", "review", ContentType.Book)],
            Movies = [MakeMovie("Some Movie (2020).md", new Dictionary<string, object> { ["title"] = "Some Movie" })],
        };

        CreateWriter().Write(structure);

        var book = File.ReadAllText(Path.Combine(HugoContentDir, "library", "books", "some-book.md"));
        var movie = File.ReadAllText(Path.Combine(HugoContentDir, "library", "movies-tv-shows", "some-movie-(2020).md"));
        var expectedEnd = $"[build]{Environment.NewLine}  render = \"never\"{Environment.NewLine}  list = \"always\"{Environment.NewLine}+++";
        Assert.Contains(expectedEnd, book);
        Assert.Contains(expectedEnd, movie);
    }
}
