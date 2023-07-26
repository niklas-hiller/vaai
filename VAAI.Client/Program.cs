namespace VAAI.Client;

internal class Program
{
    static async Task Main(string[] args)
        => await new CoreClient().StartAsync();
}