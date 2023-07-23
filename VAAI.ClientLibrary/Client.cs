using Microsoft.AspNetCore.SignalR.Client;
using VAAI.Shared.Communication;

namespace VAAI.ClientLibrary;

public class Client
{
    public HubConnection Connection { get; set; }
    private string Name { get; set; }
    private List<string> Groups { get; set; }

    public Client(string name)
    {
        Name = name;
        Groups = new List<string>();
        Connection = new HubConnectionBuilder()
            .WithUrl("https://localhost:7180/VAAI")
            .Build();
    }

    public void registerSTT(Func<Message, Task<object>> func)
    {
        if (!Groups.Contains(Broadcasts.SpeechToText))
        {
            Groups.Add(Broadcasts.SpeechToText);
        }

        Connection.On<Message>(Broadcasts.SpeechToText, async (message) =>
        {
            await Connection.SendAsync(Broadcasts.SpeechToTextResult, new Message(message.Id, await func(message)));
        });
    }

    public void registerTTS(Func<Message, Task<object>> func)
    {
        if (!Groups.Contains(Broadcasts.TextToSpeech))
        {
            Groups.Add(Broadcasts.TextToSpeech);
        }

        Connection.On<Message>(Broadcasts.TextToSpeech, async (message) =>
        {
            await Connection.SendAsync(Broadcasts.TextToSpeechResult, new Message(message.Id, await func(message)));
        });
    }

    public void registerLLM(Func<Message, Task<object>> func)
    {
        if (!Groups.Contains(Broadcasts.TextToText))
        {
            Groups.Add(Broadcasts.TextToText);
        }

        Connection.On<Message>(Broadcasts.TextToText, async (message) =>
        {
            await Connection.SendAsync(Broadcasts.TextToTextResult, new Message(message.Id, await func(message)));
        });
    }

    public async Task StartAsync()
    {
        try
        {
            await Connection.StartAsync();
            await Connection.SendAsync(Broadcasts.SessionConnect, new Session(this.Name, this.Groups.ToArray()));

            await Task.Delay(Timeout.Infinite);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            await Connection.StopAsync();
            await Connection.DisposeAsync();
        }
    }
}