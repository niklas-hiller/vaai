using VAAI.Library;

namespace VAAI.TTS;

internal class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
        var client = new HubClient("Some TTS");
    }
}