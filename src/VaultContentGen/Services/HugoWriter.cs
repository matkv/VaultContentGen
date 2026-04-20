using VaultContentGen.Config;
using VaultContentGen.Models;

namespace VaultContentGen.Services;

public class HugoWriter(AppConfig config)
{
    public void Write(ObsidianStructure structure)
    {
        if (Directory.Exists(config.HugoContentPath))
            Directory.Delete(config.HugoContentPath, recursive: true);

        Directory.CreateDirectory(config.HugoContentPath);

        if (structure.RootIndex is not null)
            WriteSectionIndex(structure.RootIndex, "Home", config.HugoContentPath);

        foreach (var file in structure.StandaloneFiles)
        {
            var outputPath = Path.Combine(config.HugoContentPath, ToSlug(file.FileName) + ".md");
            WriteContentFile(file, outputPath);
        }

        foreach (var section in structure.Sections)
            WriteSection(section, config.HugoContentPath, section.Name);
    }

    private void WriteSection(ObsidianSection section, string parentPath, string relativePath)
    {
        var sectionPath = Path.Combine(parentPath, section.Name.ToLower());
        Directory.CreateDirectory(sectionPath);

        WriteSectionIndex(section.SectionIndex, section.Name, sectionPath);

        foreach (var file in section.SectionFiles)
        {
            var outputPath = Path.Combine(sectionPath, ToSlug(file.FileName) + ".md");
            WriteContentFile(file, outputPath);
        }

        foreach (var sub in section.SubSections)
            WriteSection(sub, sectionPath, $"{relativePath}/{sub.Name}");
    }

    private void WriteSectionIndex(ObsidianFile? index, string sectionName, string outputPath)
    {
        var title = index is not null
            ? GetString(index, "title", sectionName)
            : sectionName;

        var description = index is not null
            ? GetString(index, "description", string.Empty)
            : string.Empty;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("+++");
        sb.AppendLine($"title = \"{title}\"");
        if (!string.IsNullOrEmpty(description))
            sb.AppendLine($"description = \"{description}\"");
        sb.AppendLine("+++");

        if (index is not null && !string.IsNullOrEmpty(index.Body))
        {
            sb.AppendLine();
            sb.Append(index.Body);
        }

        File.WriteAllText(Path.Combine(outputPath, "_index.md"), sb.ToString());
    }

    private void WriteContentFile(ObsidianFile file, string outputPath)
    {
        var frontmatter = BuildTomlFrontmatter(file);
        var content = frontmatter + Environment.NewLine + file.Body;
        File.WriteAllText(outputPath, content);
    }

    private string BuildTomlFrontmatter(ObsidianFile file)
    {
        var title = GetString(file, "title", Path.GetFileNameWithoutExtension(file.FileName));
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("+++");
        sb.AppendLine($"title = \"{title}\"");

        var description = GetString(file, "description", string.Empty);
        if (!string.IsNullOrEmpty(description))
            sb.AppendLine($"description = \"{description}\"");

        var date = GetString(file, "date", string.Empty);
        if (!string.IsNullOrEmpty(date))
            sb.AppendLine($"date = \"{date}\"");

        if (file.Type == ContentType.Book)
        {
            AppendIfPresent(sb, file, "author");
            AppendIfPresent(sb, file, "isbn");
            AppendIfPresent(sb, file, "rating");
            AppendIfPresent(sb, file, "year");
            AppendIfPresent(sb, file, "status");
        }

        sb.AppendLine("+++");
        return sb.ToString();
    }

    private static void AppendIfPresent(System.Text.StringBuilder sb, ObsidianFile file, string key)
    {
        var value = GetString(file, key, string.Empty);
        if (!string.IsNullOrEmpty(value))
            sb.AppendLine($"{key} = \"{value}\"");
    }

    private static string GetString(ObsidianFile file, string key, string fallback) =>
        file.FrontMatter.TryGetValue(key, out var value)
            ? value?.ToString() ?? fallback
            : fallback;

    public static string ToSlug(string fileName) =>
        Path.GetFileNameWithoutExtension(fileName)
            .ToLower()
            .Replace(" ", "-");
}