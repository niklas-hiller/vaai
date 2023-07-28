using Azure;
using Azure.AI.OpenAI;
using VAAI.Library;
using VAAI.Shared.Communication;
using VAAI.Shared.Enums;
using static System.Environment;

namespace VAAI.LLM;

internal class OpenAiClient
{
    private const string CLIENT_NAME = "OpenAi";
    private readonly string Model;
    private readonly OpenAIClient Client;

    internal OpenAiClient(string model)
    {
        Model = model;

        string endpoint = GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
            ?? throw new ArgumentNullException("Couldn't find AZURE_OPENAI_ENDPOINT in environment variables.");
        string key = GetEnvironmentVariable("AZURE_OPENAI_KEY")
            ?? throw new ArgumentNullException("Couldn't find AZURE_OPENAI_KEY in environment variables.");
        Client = new(new Uri(endpoint), new AzureKeyCredential(key));
    }

    private async Task Process(TaskQueue<string, string> queue)
    {
        await queue.Next(async (input) =>
        {
            var chatCompletionsOptions = new ChatCompletionsOptions()
            {
                Messages =
                {
                    new ChatMessage(ChatRole.System, "You are a helpful assistant."),
                    new ChatMessage(ChatRole.User, "Does Azure OpenAI support customer managed keys?") { Name = "same" },
                    new ChatMessage(ChatRole.Assistant, "Yes, customer managed keys are supported by Azure OpenAI."),
                    new ChatMessage(ChatRole.User, "Do other Azure AI services support this too?") { Name = "same" },
                    },
                MaxTokens = 100
            };

            Response<StreamingChatCompletions> response = await Client.GetChatCompletionsStreamingAsync(
                deploymentOrModelName: Model, chatCompletionsOptions);
            using StreamingChatCompletions streamingChatCompletions = response.Value;

            await foreach (StreamingChatChoice choice in streamingChatCompletions.GetChoicesStreaming())
            {
                await foreach (ChatMessage message in choice.GetMessageStreaming())
                {
                    Console.Write(message.Content);
                }
                Console.WriteLine();
            }

            return new Result<string>(EStatus.DROPPED, "");
        });
    }

    internal async Task StartAsync()
    {
        var client = new HubClient(CLIENT_NAME);
        var queue = client.RegisterProcessor<string, string>(EProcessor.LLM);
        await client.StartAsync();

        queue.OnInputAsync(Process);

        await Task.Delay(-1);
    }
}
