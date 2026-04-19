using VaultContentGen.Config;
using VaultContentGen.Models;
using YamlDotNet.Serialization;

namespace VaultContentGen.Services;

public class VaultScanner(AppConfig config)
{
    private static readonly IDeserializer YamlDeserializer = new DeserializerBuilder().Build();

    public ObsidianStructure Scan()
    {
        var rootPath = config.VaultSourcePath;

        ObsidianFile? rootIndex = null;
        var standaloneFiles = new List<ObsidianFile>();
        var sections = new List<ObsidianSection>();

        foreach (var entry in Directory.GetFileSystemEntries(rootPath))
        {
            if (Directory.Exists(entry))
            {
                var folderName = Path.GetFileName(entry);
                if (config.IgnoredFolders.Contains(folderName))
                    continue;

                sections.Add(ScanSection(entry, folderName));
            }
            else if (entry.EndsWith(".md"))
            {
                var file = ScanFile(entry, ContentType.Standard);
                if (Path.GetFileName(entry) == "Index.md")
                    rootIndex = file;
                else
                    standaloneFiles.Add(file);
            }
        }

        return new ObsidianStructure
        {
            RootIndex = rootIndex,
            StandaloneFiles = standaloneFiles,
            Sections = sections
        };
    }

    private ObsidianSection ScanSection(string sectionPath, string relativePath)
    {
        var contentType = ResolveContentType(relativePath);
        ObsidianFile? sectionIndex = null;
        var files = new List<ObsidianFile>();
        var subSections = new List<ObsidianSection>();

        foreach (var entry in Directory.GetFileSystemEntries(sectionPath))
        {
            if (Directory.Exists(entry))
            {
                var subFolderName = Path.GetFileName(entry);
                if (config.IgnoredFolders.Contains(subFolderName))
                    continue;

                subSections.Add(ScanSection(entry, $"{relativePath}/{subFolderName}"));
            }
            else if (entry.EndsWith(".md"))
            {
                var file = ScanFile(entry, contentType);
                if (Path.GetFileName(entry) == "Index.md")
                    sectionIndex = file;
                else
                    files.Add(file);
            }
        }

        return new ObsidianSection
        {
            Name = Path.GetFileName(sectionPath),
            SourcePath = sectionPath,
            Type = contentType,
            SectionIndex = sectionIndex,
            SectionFiles = files,
            SubSections = subSections
        };
    }

    private ContentType ResolveContentType(string relativePath) =>
        config.SectionTypes.TryGetValue(relativePath, out var type)
            && Enum.TryParse<ContentType>(type, out var parsed)
            ? parsed
            : ContentType.Standard;

    private ObsidianFile ScanFile(string filePath, ContentType contentType)
    {
        var raw = File.ReadAllText(filePath);
        var (frontmatter, body) = ParseFrontmatter(raw);

        return new ObsidianFile
        {
            FileName = Path.GetFileName(filePath),
            SourcePath = filePath,
            Body = body,
            FrontMatter = frontmatter,
            Type = contentType
        };
    }

    private static (Dictionary<string, object> frontmatter, string body) ParseFrontmatter(string content)
    {
        if (!content.StartsWith("---"))
            return ([], content);

        var end = content.IndexOf("---", 3);
        if (end == -1)
            return ([], content);

        var yaml = content[3..end].Trim();
        var body = content[(end + 3)..].Trim();
        var frontmatter = YamlDeserializer.Deserialize<Dictionary<string, object>>(yaml) ?? [];

        return (frontmatter, body);
    }
}