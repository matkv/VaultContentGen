using System.CommandLine;
using VaultContentGen.Config;
using VaultContentGen.Services;

namespace VaultContentGen.Commands;

public static class GenerateCommand
{
    public static Command Create(ConfigService configService)
    {
        var command = new Command("generate", "Generate Hugo content from the Obsidian vault");

        command.SetAction(_ =>
        {
            try
            {
                var config = configService.Load();

                if (string.IsNullOrEmpty(config.VaultSourcePath))
                {
                    Console.WriteLine("Vault source path not configured. Run 'config set' first.");
                    return;
                }

                if (string.IsNullOrEmpty(config.HugoContentPath))
                {
                    Console.WriteLine("Hugo content path not configured. Run 'config set' first.");
                    return;
                }

                var scanner = new VaultScanner(config);
                var structure = scanner.Scan();

                var writer = new HugoWriter(config);
                writer.Write(structure);

                Console.WriteLine("Done.");

            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        });

        return command;
    }
}