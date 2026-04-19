using System.CommandLine;
using VaultContentGen.Config;
using VaultContentGen.Models;
using VaultContentGen.Services;

namespace VaultContentGen.Commands;

public static class ScanCommand
{
    public static Command Create(ConfigService configService)
    {
        var command = new Command("scan", "Scan and display the vault structure");

        command.SetAction(_ =>
        {
            var config = configService.Load();

            if (string.IsNullOrEmpty(config.VaultSourcePath))
            {
                Console.WriteLine("Vault source path not configured. Run 'config set' first.");
                return;
            }

            var scanner = new VaultScanner(config);
            var structure = scanner.Scan();

            PrintStructure(structure);
        });

        return command;
    }

    private static void PrintStructure(ObsidianStructure structure)
    {
        if (structure.RootIndex is not null)
            Console.WriteLine($"[ROOT INDEX] {structure.RootIndex.FileName}");

        foreach (var file in structure.StandaloneFiles)
            Console.WriteLine($"[PAGE] {file.FileName}");

        foreach (var section in structure.Sections)
            PrintSection(section, 0);
    }

    private static void PrintSection(ObsidianSection section, int depth)
    {
        var indent = new string(' ', depth * 2);
        Console.WriteLine($"{indent}[SECTION:{section.Type}] {section.Name}");

        if (section.SectionIndex is not null)
            Console.WriteLine($"{indent}  [INDEX] {section.SectionIndex.FileName}");

        foreach (var file in section.SectionFiles)
            Console.WriteLine($"{indent}  [FILE] {file.FileName}");

        foreach (var sub in section.SubSections)
            PrintSection(sub, depth + 1);
    }
}