namespace VAAI.Discord;

internal class Program
{
    static async Task Main(string[] args)
        => await new DiscordClient().StartAsync();
}