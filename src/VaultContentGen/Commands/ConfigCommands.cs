using System.CommandLine;
using VaultContentGen.Config;

namespace VaultContentGen.Commands;

public static class ConfigCommands
{
    public static Command Create(ConfigService configService)
    {
        var command = new Command("config", "Manage configuration");

        command.Subcommands.Add(ConfigSetCommand.Create(configService));
        command.Subcommands.Add(CreateShowCommand(configService));

        return command;
    }

    private static Command CreateShowCommand(ConfigService configService)
    {
        var command = new Command("show", "Display current configuration");

        command.SetAction(_ =>
        {
            var config = configService.Load();
            Console.WriteLine($"Vault source: {config.VaultSourcePath}");
            Console.WriteLine($"Hugo content: {config.HugoContentPath}");
        });

        return command;
    }
}