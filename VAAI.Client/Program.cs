namespace VAAI.Client
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var audioClient = new AudioClient(16000, 1);
            await audioClient.StartAsync();

            await Task.Delay(-1);
        }
    }
}