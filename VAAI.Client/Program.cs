namespace VAAI.Client
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello, World!");

            var audioClient = new AudioClient(16000, 1);

            await audioClient.StartAsync();
        }
    }
}