using Microsoft.AspNetCore.SignalR.Client;
using VAAI.Shared.Communication;

namespace VAAI.Library;

public class Invoker
{
    private HubClient Client { get; set; }

    public Invoker(HubClient Client)
    {
        this.Client = Client;
    }

    public async Task<Guid> InvokeTTS(string message)
    {
        if (!Client.IsActive)
        {
            throw new NotSupportedException($"You can't invoke TTS without active Hub Client Connection.");
        }

        var id = await Client.Connection.InvokeAsync<Guid>(Broadcasts.TextToSpeech, message);
        return id;
    }

    public async Task<Guid> InvokeSTT(float[] samples)
    {
        if (!Client.IsActive)
        {
            throw new NotSupportedException($"You can't invoke STT without active Hub Client Connection.");
        }

        var id = await Client.Connection.InvokeAsync<Guid>(Broadcasts.SpeechToText, samples);
        return id;
    }

    public async Task<Guid> InvokeLLM(string message)
    {
        if (!Client.IsActive)
        {
            throw new NotSupportedException($"You can't invoke LLM without active Hub Client Connection.");
        }

        var id = await Client.Connection.InvokeAsync<Guid>(Broadcasts.TextToText, message);
        return id;
    }
}
