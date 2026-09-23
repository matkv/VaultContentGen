using System.CommandLine;
using VaultContentGen.Config;
using VaultContentGen.Services;

namespace VaultContentGen.Commands;

public static class GenerateCommand
{
    public static Command Create(ConfigService configService)
    {
        var noServeOption = new Option<bool>("--no-serve")
        {
            Description = "Only generate the content; don't clear public/ or start hugo serve"
        };

        var command = new Command("generate", "Generate Hugo content from the Obsidian vault")
        {
            noServeOption
        };

        command.SetAction(parseResult =>
        {
            AppConfig config;
            try
            {
                config = configService.Load();
            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return 1;
            }

            var exitCode = GenerateContent(config);
            if (exitCode != 0)
                return exitCode;

            if (parseResult.GetValue(noServeOption))
            {
                Console.WriteLine($"Content generated in: {config.HugoContentPath}");
                return 0;
            }

            return Serve(config);
        });

        return command;
    }

    public static int GenerateContent(AppConfig config)
    {
        if (string.IsNullOrEmpty(config.VaultSourcePath))
        {
            Console.WriteLine("Vault source path not configured. Run 'config set' first.");
            return 1;
        }

        if (string.IsNullOrEmpty(config.HugoContentPath))
        {
            Console.WriteLine("Hugo content path not configured. Run 'config set' first.");
            return 1;
        }

        try
        {
            var scanner = new VaultScanner(config);
            var structure = scanner.Scan();

            var writer = new HugoWriter(config);
            writer.Write(structure);
            return 0;
        }
        catch (System.Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private static int Serve(AppConfig config)
    {
        try
        {
            var hugoSitePath = Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(Path.GetFullPath(config.HugoContentPath)))!;
            var publicPath = Path.Combine(hugoSitePath, "public");

            if (Directory.Exists(publicPath))
            {
                Console.WriteLine("Clearing public directory...");
                Directory.Delete(publicPath, recursive: true);
            }

            Console.WriteLine($"Running hugo serve in: {hugoSitePath}");
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "hugo",
                    Arguments = "serve",
                    WorkingDirectory = hugoSitePath,
                    UseShellExecute = false,
                }
            };
            process.Start();
            process.WaitForExit();
            return 0;
        }
        catch (System.Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
}
