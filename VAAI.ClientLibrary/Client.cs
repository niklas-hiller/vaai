﻿using Microsoft.AspNetCore.SignalR.Client;
using VAAI.Shared.Communication;
using VAAI.Shared.Enums;

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

    public void registerTTS(Func<Message<string>, Task<float[]>> func)
    {
        if (!Groups.Contains(SessionGroups.TTS_AI))
        {
            Groups.Add(SessionGroups.TTS_AI);
        }

        Connection.On<Message<string>>(Broadcasts.TextToSpeech, async (message) =>
        {
            await Connection.SendAsync(Broadcasts.TextToSpeechResult, new Message<float[]>(message.Id, await func(message)));
        });
    }

    public void registerSTT(Func<Message<float[]>, Task<string>> func)
    {
        if (!Groups.Contains(SessionGroups.STT_AI))
        {
            Groups.Add(SessionGroups.STT_AI);
        }

        Connection.On<Message<float[]>>(Broadcasts.SpeechToText, async (message) =>
        {
            await Connection.SendAsync(Broadcasts.SpeechToTextResult, new Message<string>(message.Id, await func(message)));
        });
    }

    public void registerLLM(Func<Message<string>, Task<string>> func)
    {
        if (!Groups.Contains(SessionGroups.LLM_AI))
        {
            Groups.Add(SessionGroups.LLM_AI);
        }

        Connection.On<Message<string>>(Broadcasts.TextToText, async (message) =>
        {
            await Connection.SendAsync(Broadcasts.TextToTextResult, new Message<string>(message.Id, await func(message)));
        });
    }

    public void registerListener(Func<Message<object>, Task<object>> func, EListener whichListener)
    {
        if (!Groups.Contains(SessionGroups.Listener))
        {
            Groups.Add(SessionGroups.Listener);
        }

        switch (whichListener)
        {
            case EListener.TTS:
                Connection.On<Message<object>>(Broadcasts.TextToSpeechResult, async (message) =>
                {
                    await func(message);
                });
                break;
            case EListener.STT:
                Connection.On<Message<object>>(Broadcasts.SpeechToTextResult, async (message) =>
                {
                    await func(message);
                });
                break;
            case EListener.LLM:
                Connection.On<Message<object>>(Broadcasts.TextToTextResult, async (message) =>
                {
                    await func(message);
                });
                break;
        }
    }

    public void registerInvoker(Func<Message<object>, Task<object>> func)
    {
        if (!Groups.Contains(SessionGroups.Invoker))
        {
            Groups.Add(SessionGroups.Invoker);
        }
    }

    public async Task<Guid> InvokeTTS(string message)
    {
        return await Connection.InvokeAsync<Guid>(Broadcasts.TextToSpeech, message);
    }

    public async Task<Guid> InvokeSTT(float[] samples)
    {
        return await Connection.InvokeAsync<Guid>(Broadcasts.SpeechToText, samples);
    }

    public async Task<Guid> InvokeLLM(string message)
    {
        return await Connection.InvokeAsync<Guid>(Broadcasts.TextToText, message);
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