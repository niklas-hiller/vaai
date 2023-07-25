using Microsoft.AspNetCore.SignalR.Client;
using VAAI.Shared.Communication;

namespace VAAI.Library;

public class Listener
{
    private HubClient Client { get; set; }

    public Listener(HubClient Client)
    {
        this.Client = Client;
    }

    public void OnTTS(Func<Message<Result<float[]>>, Task> func)
    {
        Client.Connection.On<Message<Result<float[]>>>(Broadcasts.TextToSpeechResult, async (message) =>
        {
            await func(message);
        });
    }

    public void OnSTT(Func<Message<Result<string>>, Task> func)
    {
        Client.Connection.On<Message<Result<string>>>(Broadcasts.SpeechToTextResult, async (message) =>
        {
            await func(message);
        });
    }

    public void OnLLM(Func<Message<Result<string>>, Task> func)
    {
        Client.Connection.On<Message<Result<string>>>(Broadcasts.TextToTextResult, async (message) =>
        {
            await func(message);
        });
    }
}
