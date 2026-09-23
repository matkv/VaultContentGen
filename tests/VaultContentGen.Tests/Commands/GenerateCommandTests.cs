using VaultContentGen.Commands;
using VaultContentGen.Config;

namespace VaultContentGen.Tests.Commands;

public class GenerateCommandTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    private string VaultDir => Path.Combine(_tempDir, "vault");
    private string HugoSiteDir => Path.Combine(_tempDir, "site");
    private string HugoContentDir => Path.Combine(HugoSiteDir, "content");
    private string ConfigPath => Path.Combine(_tempDir, "config.json");

    private ConfigService CreateConfigService(AppConfig config)
    {
        var service = new ConfigService(ConfigPath);
        service.Save(config);
        return service;
    }

    private AppConfig ValidConfig() => new()
    {
        VaultSourcePath = VaultDir,
        HugoContentPath = HugoContentDir,
    };

    private void CreateVaultNote(string name, string content)
    {
        Directory.CreateDirectory(VaultDir);
        File.WriteAllText(Path.Combine(VaultDir, name), content);
    }

    private static int Invoke(ConfigService configService, params string[] args) =>
        GenerateCommand.Create(configService).Parse(args).Invoke();

    public void Dispose()
    {
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void NoServe_WritesContentAndReturnsZero()
    {
        CreateVaultNote("Post.md", "---\ntitle: Post\n---\nHello");
        var configService = CreateConfigService(ValidConfig());

        var exitCode = Invoke(configService, "--no-serve");

        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(Path.Combine(HugoContentDir, "post.md")));
    }

    [Fact]
    public void NoServe_DoesNotDeletePublicDirectory()
    {
        CreateVaultNote("Post.md", "---\ntitle: Post\n---\nHello");
        var publicFile = Path.Combine(HugoSiteDir, "public", "index.html");
        Directory.CreateDirectory(Path.GetDirectoryName(publicFile)!);
        File.WriteAllText(publicFile, "<html></html>");
        var configService = CreateConfigService(ValidConfig());

        Invoke(configService, "--no-serve");

        Assert.True(File.Exists(publicFile));
    }

    [Fact]
    public void NoServe_MissingVaultPath_ReturnsNonZero()
    {
        var configService = CreateConfigService(ValidConfig() with { VaultSourcePath = string.Empty });

        Assert.NotEqual(0, Invoke(configService, "--no-serve"));
    }

    [Fact]
    public void NoServe_MissingHugoPath_ReturnsNonZero()
    {
        var configService = CreateConfigService(ValidConfig() with { HugoContentPath = string.Empty });

        Assert.NotEqual(0, Invoke(configService, "--no-serve"));
    }

    [Fact]
    public void NoServe_NonExistentVault_ReturnsNonZero()
    {
        var configService = CreateConfigService(ValidConfig());

        Assert.NotEqual(0, Invoke(configService, "--no-serve"));
    }

    [Fact]
    public void NoServe_InvalidConfigJson_ReturnsNonZero()
    {
        Directory.CreateDirectory(_tempDir);
        File.WriteAllText(ConfigPath, "{ not json");

        Assert.NotEqual(0, Invoke(new ConfigService(ConfigPath), "--no-serve"));
    }
}
