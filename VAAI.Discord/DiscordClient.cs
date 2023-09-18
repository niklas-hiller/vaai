using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VAAI.Discord.Handler;

namespace VAAI.Discord;

internal class DiscordClient
{
    private readonly ILogger logger;
    private readonly DiscordSocketClient client;
    private readonly ICommandHandler command;
    private readonly IConfigurationRoot config;

    public DiscordClient(ILogger<DiscordClient> logger, DiscordSocketClient client, ICommandHandler command)
    {
        this.logger = logger;
        this.client = client;
        this.command = command;

        config = new ConfigurationBuilder()
            .AddUserSecrets<DiscordClient>()
            .Build();
    }

    private Task Log(LogMessage msg)
    {
        logger.LogInformation(msg.ToString());
        return Task.CompletedTask;
    }

    private async Task Ready()
    {
        await command.Initialize();
    }

    public async Task StartAsync()
    {
        logger.LogInformation("Starting Application...");

        client.Log += Log;
        client.Ready += Ready;

        #region Login
        var token = config.GetValue<string>("token");
        if (token is null)
        {
            throw new ArgumentNullException(nameof(token));
        }
        await client.LoginAsync(TokenType.Bot, token);
        #endregion
        await client.StartAsync();

        await Task.Delay(-1);
    }
}
