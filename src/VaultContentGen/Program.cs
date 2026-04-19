using System.CommandLine;
using VaultContentGen.Commands;
using VaultContentGen.Config;

var configService = ConfigService.CreateDefault();

var rootCommand = new RootCommand("Generate Hugo content from an Obsidian vault");
rootCommand.Subcommands.Add(ConfigCommands.Create(configService));
rootCommand.Subcommands.Add(ScanCommand.Create(configService));

await rootCommand.Parse(args).InvokeAsync();