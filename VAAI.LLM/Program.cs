using VAAI.ClientLibrary;

namespace VAAI.LLM;

internal class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
        var client = new Client("Some LLM");
    }
}