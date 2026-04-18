using VaultContentGen.Config;

var configService = ConfigService.CreateDefault();
var config = configService.Load();

Console.WriteLine($"Vault souce: {config.VaultSourcePath}");
Console.WriteLine($"Hugo content: {config.HugoContentPath}");