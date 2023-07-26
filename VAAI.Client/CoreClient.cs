using Microsoft.Extensions.Logging;

namespace VAAI.Client;

internal class CoreClient
{
    private readonly ILogger logger;
    private readonly AudioClient AudioClient;

    public CoreClient()
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddFilter("Microsoft", LogLevel.Warning)
                .AddFilter("System", LogLevel.Warning)
                .AddConsole();
        });
        logger = loggerFactory.CreateLogger<CoreClient>();

        this.AudioClient = new AudioClient(16000, 1);
    }

    public async Task StartAsync()
    {
        logger.LogInformation("Starting Application...");

        await AudioClient.StartAsync();

        await Task.Delay(-1);
    }
}
