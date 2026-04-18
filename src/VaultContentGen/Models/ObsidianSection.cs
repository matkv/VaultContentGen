namespace VaultContentGen.Models;

public record ObsidianSection
{
    public string Name { get; init; } = string.Empty;
    public string SourcePath { get; init; } = string.Empty;
    public ContentType Type { get; init; } = ContentType.Standard;
    public ObsidianFile? SectionIndex { get; init; }
    public List<ObsidianFile> SectionFiles { get; init; } = [];
    public List<ObsidianSection> SubSections { get; init; } = [];
}