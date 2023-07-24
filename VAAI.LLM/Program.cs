using VAAI.Library;

namespace VAAI.LLM;

internal class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
        var client = new HubClient("Some LLM");
    }
}