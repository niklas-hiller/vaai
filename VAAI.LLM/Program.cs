namespace VAAI.LLM;

internal class Program
{
    static async Task Main(string[] args)
        => await new OpenAiClient("gpt-35-turbo").StartAsync();
}