namespace VaultContentGen.Config;

public record AppConfig
{
    public string VaultSourcePath { get; init; } = string.Empty;
    public string HugoContentPath { get; init; } = string.Empty;
    public List<string> IgnoredFolders { get; init; } = [];
    public Dictionary<string, string> SectionTypes { get; init; } = [];
}