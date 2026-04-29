namespace VaultContentGen.Models;

public record ObsidianStructure
{
    public ObsidianFile? RootIndex { get; init; }
    public List<ObsidianFile> StandaloneFiles { get; init; } = [];
    public List<ObsidianSection> Sections { get; init; } = [];
    public List<ObsidianFile> Books { get; init; } = [];
}