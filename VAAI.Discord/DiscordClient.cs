using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace VAAI.Discord;

internal class DiscordClient
{
    private readonly ILogger logger;

    public readonly DiscordSocketClient client = new DiscordSocketClient();

    private readonly IConfigurationRoot config;

    public DiscordClient()
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddFilter("Microsoft", LogLevel.Warning)
                .AddFilter("System", LogLevel.Warning)
                .AddConsole();
        });
        logger = loggerFactory.CreateLogger<DiscordClient>();

        config = new ConfigurationBuilder()
            .AddUserSecrets<DiscordClient>()
            .Build();
    }

    private Task Log(LogMessage msg)
    {
        logger.LogInformation(msg.ToString());
        return Task.CompletedTask;
    }

    public async Task StartAsync()
    {
        logger.LogInformation("Starting Application...");

        client.Log += Log;

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
