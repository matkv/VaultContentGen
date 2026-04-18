namespace VaultContentGen.Models;

public record ObsidianFile
{
    public string FileName { get; init; } = string.Empty;
    public string SourcePath { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public Dictionary<string, object> FrontMatter { get; init; } = [];
    public ContentType Type { get; init; } = ContentType.Standard;
}