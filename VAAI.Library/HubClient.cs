using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using VAAI.Shared.Communication;
using VAAI.Shared.Enums;

namespace VAAI.Library;

public class HubClient
{
    public HubConnection Connection { get; set; }
    private string Name { get; set; }
    private List<string> Groups { get; set; }
    private List<Task> AsyncTasks { get; set; } = new List<Task>();
    private readonly ILogger logger;
    public bool IsActive { get; set; } = false;

    public HubClient(string name)
    {
        Name = name;
        Groups = new List<string>();
        Connection = new HubConnectionBuilder()
            .WithUrl("https://localhost:7180/VAAI")
            .Build();

        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddFilter("Microsoft", LogLevel.Warning)
                .AddFilter("System", LogLevel.Warning)
                .AddConsole();
        });
        logger = loggerFactory.CreateLogger<HubClient>();

        Connection.On(Broadcasts.SessionConnect, () =>
        {
            _ = Task.Run(async () =>
            {
                logger.LogInformation("Successfully created session.");
                AsyncTasks.ForEach((task) =>
                {
                    task.Start();
                });
                IsActive = true;
                await Task.Delay(-1);
            });
        });
    }

    public TaskQueue<string, float[]> registerTTS()
    {
        if (Groups.Contains(SessionGroups.TTS_AI))
        {
            throw new NotSupportedException($"You can't register the {SessionGroups.TTS_AI} multiple times.");
        }
        logger.LogInformation($"Register Hub Client as TTS AI.");
        Groups.Add(SessionGroups.TTS_AI);

        var messageQueue = new MessageQueue<string, float[]>();
        Connection.On<Message<string>>(Broadcasts.TextToSpeech, messageQueue.Enqueue);
        var task = new Task(async () =>
        {
            while (true)
            {
                if (messageQueue.HasFinishedTasks)
                {
                    await Connection.SendAsync(Broadcasts.TextToSpeechResult, messageQueue.Dequeue());
                }
            }
        });
        AsyncTasks.Add(task);

        return messageQueue.Tasks;
    }

    public TaskQueue<float[], string> registerSTT()
    {
        if (Groups.Contains(SessionGroups.STT_AI))
        {
            throw new NotSupportedException($"You can't register the {SessionGroups.STT_AI} multiple times.");
        }
        logger.LogInformation($"Register Hub Client as STT AI.");
        Groups.Add(SessionGroups.STT_AI);

        var messageQueue = new MessageQueue<float[], string>();
        Connection.On<Message<float[]>>(Broadcasts.SpeechToText, messageQueue.Enqueue);
        var task = new Task(async () =>
        {
            while (true)
            {
                if (messageQueue.HasFinishedTasks)
                {
                    await Connection.SendAsync(Broadcasts.SpeechToTextResult, messageQueue.Dequeue());
                }
            }
        });
        AsyncTasks.Add(task);

        return messageQueue.Tasks;
    }

    public TaskQueue<string, string> registerLLM()
    {
        if (Groups.Contains(SessionGroups.LLM_AI))
        {
            throw new NotSupportedException($"You can't register the {SessionGroups.LLM_AI} multiple times.");
        }
        logger.LogInformation($"Register Hub Client as LLM AI.");
        Groups.Add(SessionGroups.LLM_AI);

        var messageQueue = new MessageQueue<string, string>();
        Connection.On<Message<string>>(Broadcasts.TextToText, messageQueue.Enqueue);
        var task = new Task(async () =>
        {
            while (true)
            {
                if (messageQueue.HasFinishedTasks)
                {
                    await Connection.SendAsync(Broadcasts.TextToTextResult, messageQueue.Dequeue());
                }
            }
        });
        AsyncTasks.Add(task);

        return messageQueue.Tasks;
    }

    public void registerListener<T>(EListener whichListener, Func<Message<Result<T>>, Task> func)
    {
        if (!Groups.Contains(SessionGroups.Listener))
        {
            logger.LogInformation($"Register Hub Client as Listener.");
            Groups.Add(SessionGroups.Listener);
        }

        switch (whichListener)
        {
            case EListener.TTS:
                Connection.On<Message<Result<T>>>(Broadcasts.TextToSpeechResult, async (message) =>
                {
                    await func(message);
                });
                break;
            case EListener.STT:
                Connection.On<Message<Result<T>>>(Broadcasts.SpeechToTextResult, async (message) =>
                {
                    await func(message);
                });
                break;
            case EListener.LLM:
                Connection.On<Message<Result<T>>>(Broadcasts.TextToTextResult, async (message) =>
                {
                    await func(message);
                });
                break;
        }
    }

    public void registerInvoker()
    {
        if (!Groups.Contains(SessionGroups.Invoker))
        {
            logger.LogInformation($"Register Hub Client as Invoker.");
            Groups.Add(SessionGroups.Invoker);
        }
    }

    public async Task<Guid> InvokeTTS(string message)
    {
        var id = await Connection.InvokeAsync<Guid>(Broadcasts.TextToSpeech, message);
        logger.LogInformation($"Invoked TTS {id}");
        return id;
    }

    public async Task<Guid> InvokeSTT(float[] samples)
    {
        var id = await Connection.InvokeAsync<Guid>(Broadcasts.SpeechToText, samples);
        logger.LogInformation($"Invoked STT {id}");
        return id;
    }

    public async Task<Guid> InvokeLLM(string message)
    {
        var id = await Connection.InvokeAsync<Guid>(Broadcasts.TextToText, message);
        logger.LogInformation($"Invoked LLM {id}");
        return id;
    }

    public async Task StartAsync()
    {
        _ = Task.Run(async () =>
        {
            logger.LogInformation("Starting Hub Client...");
            await Connection.StartAsync();
            await Connection.SendAsync(Broadcasts.SessionConnect, new Session(this.Name, this.Groups.ToArray()));
            await Task.Delay(-1);
        });

        while (!IsActive)
        {
            await Task.Delay(100);
        }
    }
}