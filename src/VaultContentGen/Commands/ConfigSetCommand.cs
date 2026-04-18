using System.CommandLine;
using VaultContentGen.Config;

namespace VaultContentGen.Commands;

public static class ConfigSetCommand
{
    public static Command Create(ConfigService configService)
    {

        var vaultPathOption = new Option<string>("--vault-path")
        {
            Description = "Path to the Obsidian vault folder",
            Required = true
        };

        var hugoPathOption = new Option<string>("--hugo-path")
        {
            Description = "Path to the Hugo content directory",
            Required = true
        };

        var command = new Command("set", "Set configuration paths")
        {
            vaultPathOption,
            hugoPathOption
        };

        command.SetAction(parseResult =>
        {
            var vaultPath = parseResult.GetValue(vaultPathOption)!;
            var hugoPath = parseResult.GetValue(hugoPathOption)!;

            var updated = new AppConfig
            {
                VaultSourcePath = vaultPath,
                HugoContentPath = hugoPath
            };

            configService.Save(updated);
            Console.WriteLine("Configuration saved.");
        });

        return command;
    }
}