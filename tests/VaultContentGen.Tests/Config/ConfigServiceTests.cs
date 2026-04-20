using VaultContentGen.Config;

namespace VaultContentGen.Tests.Config;

public class ConfigServiceTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    [Fact]
    public void Load_WhenNoConfigFile_ReturnsDefaultConfig()
    {
        var sut = new ConfigService(Path.Combine(_tempDir, "config.json"));
        var config = sut.Load();

        Assert.Equal(string.Empty, config.VaultSourcePath);
        Assert.Equal(string.Empty, config.HugoContentPath);
    }

    [Fact]
    public void SaveAndLoad_RoundTrips_Config()
    {
        var sut = new ConfigService(Path.Combine(_tempDir, "config.json"));
        var original = new AppConfig
        {
            VaultSourcePath = "/home/matkv/obsidian/Notes/matkv.dev",
            HugoContentPath = "/home/matkv/mysite/content",
            IgnoredFolders = new List<string> { "TEMPCLEANUP" },
            SectionTypes = new Dictionary<string, string> { ["Library/Books"] = "Book" }
        };

        sut.Save(original);
        var loaded = sut.Load();

        Assert.Equal(original.VaultSourcePath, loaded.VaultSourcePath);
        Assert.Equal(original.HugoContentPath, loaded.HugoContentPath);
        Assert.Equal(original.IgnoredFolders, loaded.IgnoredFolders);
        Assert.Equal(original.SectionTypes, loaded.SectionTypes);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }
}