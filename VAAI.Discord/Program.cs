using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VAAI.Discord.Handler;
using VAAI.Discord.Services;

namespace VAAI.Discord;

internal class Program
{
    public static IServiceProvider ConfigureServices()
    {
        var serviceProvider = new ServiceCollection()
            .AddLogging(builder =>
            {
                builder
                    .AddFilter("Microsoft", LogLevel.Warning)
                    .AddFilter("System", LogLevel.Warning)
                    .AddConsole();
            })
            .AddScoped<ILocalizationService, LocalizationService>()
            .AddScoped<IVoiceService, VoiceService>()
            .AddScoped<ICommandHandler, CommandHandler>()
            .AddScoped<DiscordSocketClient>(provider =>
            {
                var config = new DiscordSocketConfig()
                {
                    GatewayIntents = GatewayIntents.AllUnprivileged
                };
                return new DiscordSocketClient(config);
            })
            .AddScoped<DiscordClient>()
            .BuildServiceProvider();

        return serviceProvider;
    }

    static async Task Main(string[] args)
    {
        IServiceProvider serviceProvider = ConfigureServices();
        DiscordClient client = serviceProvider.GetService<DiscordClient>() ?? throw new ArgumentNullException(nameof(DiscordClient));
        await client.StartAsync();
    }
}