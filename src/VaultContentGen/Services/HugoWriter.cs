using System.Text.RegularExpressions;
using VaultContentGen.Config;
using VaultContentGen.Models;

namespace VaultContentGen.Services;

public class HugoWriter(AppConfig config)
{
    private readonly string _hugoSitePath =
        Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(Path.GetFullPath(config.HugoContentPath)))!;

    public void Write(ObsidianStructure structure)
    {
        if (Directory.Exists(config.HugoContentPath))
            Directory.Delete(config.HugoContentPath, recursive: true);

        Directory.CreateDirectory(config.HugoContentPath);

        if (structure.RootIndex is not null)
            WriteSectionIndex(structure.RootIndex, "Home", config.HugoContentPath, ContentType.Standard);

        foreach (var file in structure.StandaloneFiles)
        {
            var outputPath = Path.Combine(config.HugoContentPath, ToSlug(file.FileName) + ".md");
            WriteContentFile(file, outputPath);
        }

        foreach (var section in structure.Sections)
            WriteSection(section, config.HugoContentPath, section.Name);

        if (structure.Books.Count > 0 || structure.BooksIndex is not null)
            WriteBooks(structure.BooksIndex, structure.Books);
    }

    private void WriteBooks(ObsidianFile? booksIndex, List<ObsidianFile> books)
    {
        var booksPath = Path.Combine(config.HugoContentPath, "library", "books");
        Directory.CreateDirectory(booksPath);

        WriteSectionIndex(booksIndex, "Books", booksPath, ContentType.Book);

        var coversDestPath = Path.Combine(_hugoSitePath, "static", "covers");
        Directory.CreateDirectory(coversDestPath);

        foreach (var book in books)
        {
            CopyBookCover(book, coversDestPath);
            var outputPath = Path.Combine(booksPath, ToSlug(book.FileName) + ".md");
            WriteContentFile(book, outputPath);
        }
    }

    private void CopyBookCover(ObsidianFile book, string coversDestPath)
    {
        if (!book.FrontMatter.TryGetValue("cover", out var coverValue) || coverValue == null)
            return;

        var sourcePath = Path.Combine(config.BookSourcePath, coverValue.ToString()!);
        if (!File.Exists(sourcePath))
            return;

        var destPath = Path.Combine(coversDestPath, Path.GetFileName(sourcePath));
        File.Copy(sourcePath, destPath, overwrite: true);
    }

    private void WriteSection(ObsidianSection section, string parentPath, string relativePath, bool isIndexContent = false, string indexPath = "")
    {
        var sectionPath = Path.Combine(parentPath, section.Name.ToLower());
        Directory.CreateDirectory(sectionPath);

        WriteSectionIndex(section.SectionIndex, section.Name, sectionPath, section.Type);

        var indexContent = isIndexContent || section.Type == ContentType.Index;
        var currentIndexPath = section.Type == ContentType.Index ? "/" : indexPath;

        foreach (var file in section.SectionFiles)
        {
            var outputPath = Path.Combine(sectionPath, ToSlug(file.FileName) + ".md");
            WriteContentFile(file, outputPath, indexContent, currentIndexPath);
        }

        var subParentPath = section.Type == ContentType.Index
            ? config.HugoContentPath
            : sectionPath;

        foreach (var sub in section.SubSections)
        {
            var subIndexPath = section.Type == ContentType.Index
                ? $"/{sub.Name.ToLower()}"
                : $"{currentIndexPath}/{sub.Name.ToLower()}";
            WriteSection(sub, subParentPath, $"{relativePath}/{sub.Name}", indexContent, subIndexPath);
        }
    }

    private void WriteSectionIndex(ObsidianFile? index, string sectionName, string outputPath, ContentType type)
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
            sb.Append(ProcessImageComments(index.Body, type));
        }

        File.WriteAllText(Path.Combine(outputPath, "_index.md"), sb.ToString());
    }

    private void WriteContentFile(ObsidianFile file, string outputPath, bool isIndexContent = false, string indexPath = "")
    {
        var frontmatter = BuildTomlFrontmatter(file, isIndexContent, indexPath);
        var body = ProcessImageComments(file.Body, file.Type);
        var content = frontmatter + Environment.NewLine + body;
        File.WriteAllText(outputPath, content);
    }

    private static readonly Regex ImageCommentRegex = new(@"<!--\s*Image:\s*(.+?)\s*-->", RegexOptions.Compiled);

    private string ProcessImageComments(string body, ContentType type)
    {
        return ImageCommentRegex.Replace(body, match =>
        {
            var relativePath = match.Groups[1].Value.Trim().Replace('\\', '/');
            var sourcePath = Path.Combine(config.VaultSourcePath, relativePath);

            if (!File.Exists(sourcePath))
                return match.Value;

            var fileName = Path.GetFileName(relativePath);
            var typeFolder = type.ToString().ToLower();
            var staticDest = Path.Combine(_hugoSitePath, "static", "images", typeFolder, fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(staticDest)!);
            File.Copy(sourcePath, staticDest, overwrite: true);

            return $"![{Path.GetFileNameWithoutExtension(fileName)}](/images/{typeFolder}/{fileName})";
        });
    }

    private string BuildTomlFrontmatter(ObsidianFile file, bool isIndexContent = false, string indexPath = "")
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
            AppendTomlArray(sb, file, "author");
            AppendIfPresent(sb, file, "isbn");
            AppendIfPresent(sb, file, "rating");
            AppendIfPresent(sb, file, "year");
            AppendTomlArray(sb, file, "status");
            AppendBookCover(sb, file);
        }

        if (file.Type == ContentType.Project)
            AppendIfPresent(sb, file, "status");

        if (isIndexContent)
        {
            sb.AppendLine("index_entry = true");
            sb.AppendLine($"section_path = \"{indexPath}\"");
        }

        if (file.Type == ContentType.Index)
            sb.AppendLine($"url = \"/{ToSlug(file.FileName)}\"");

        sb.AppendLine("+++");
        return sb.ToString();
    }

    private static void AppendIfPresent(System.Text.StringBuilder sb, ObsidianFile file, string key)
    {
        var value = GetString(file, key, string.Empty);
        if (!string.IsNullOrEmpty(value))
            sb.AppendLine($"{key} = \"{value}\"");
    }

    private static void AppendTomlArray(System.Text.StringBuilder sb, ObsidianFile file, string key)
    {
        if (!file.FrontMatter.TryGetValue(key, out var value) || value == null)
            return;

        var items = value is List<object> list
            ? list.Select(v => $"\"{v}\"")
            : [$"\"{value}\""];

        sb.AppendLine($"{key} = [{string.Join(", ", items)}]");
    }

    private static void AppendBookCover(System.Text.StringBuilder sb, ObsidianFile file)
    {
        if (!file.FrontMatter.TryGetValue("cover", out var value) || value == null)
            return;

        var fileName = Path.GetFileName(value.ToString()!);
        sb.AppendLine($"cover = \"/covers/{fileName}\"");
    }

    private static string GetString(ObsidianFile file, string key, string fallback) =>
        file.FrontMatter.TryGetValue(key, out var value)
            ? value?.ToString() ?? fallback
            : fallback;

    public static string ToSlug(string fileName) =>
        Regex.Replace(
            Path.GetFileNameWithoutExtension(fileName).ToLower().Replace(" ", "-"),
            "-{2,}", "-")
        .Trim('-');
}
