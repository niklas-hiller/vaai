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

    private void On<T>(Action<Message<Result<T>>> func, string whichBroadcast)
    {
        Client.Connection.On<Message<Result<T>>>(whichBroadcast, (message) =>
        {
            func(message);
        });
    }

    private void OnAsync<T>(Func<Message<Result<T>>, Task> func, string whichBroadcast)
    {
        Client.Connection.On<Message<Result<T>>>(whichBroadcast, async (message) =>
        {
            await func(message);
        });
    }

    public void OnTTSAsync(Func<Message<Result<float[]>>, Task> func) => OnAsync(func, Broadcasts.TextToSpeechResult);
    public void OnSTTAsync(Func<Message<Result<string>>, Task> func) => OnAsync(func, Broadcasts.SpeechToTextResult);
    public void OnLLMAsync(Func<Message<Result<string>>, Task> func) => OnAsync(func, Broadcasts.TextToTextResult);

    public void OnTTS(Action<Message<Result<float[]>>> func) => On(func, Broadcasts.TextToSpeechResult);
    public void OnSTT(Action<Message<Result<string>>> func) => On(func, Broadcasts.SpeechToTextResult);
    public void OnLLM(Action<Message<Result<string>>> func) => On(func, Broadcasts.TextToTextResult);

}
