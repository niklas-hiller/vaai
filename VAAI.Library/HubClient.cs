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
                logger.LogInformation("Successfully connected to Hub.");
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
        if (IsActive)
        {
            throw new NotSupportedException($"You can't call functions of a active Hub Client with active Connection.");
        }

        if (Groups.Contains(SessionGroups.TTS_AI))
        {
            throw new NotSupportedException($"You can't register the {SessionGroups.TTS_AI} multiple times.");
        }
        logger.LogDebug($"Register Hub Client as TTS AI.");
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
        if (IsActive)
        {
            throw new NotSupportedException($"You can't call functions of a active Hub Client with active Connection.");
        }

        if (Groups.Contains(SessionGroups.STT_AI))
        {
            throw new NotSupportedException($"You can't register the {SessionGroups.STT_AI} multiple times.");
        }
        logger.LogDebug($"Register Hub Client as STT AI.");
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
        if (IsActive)
        {
            throw new NotSupportedException($"You can't call functions of a active Hub Client with active Connection.");
        }

        if (Groups.Contains(SessionGroups.LLM_AI))
        {
            throw new NotSupportedException($"You can't register the {SessionGroups.LLM_AI} multiple times.");
        }
        logger.LogDebug($"Register Hub Client as LLM AI.");
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

    public Listener registerListener()
    {
        if (IsActive)
        {
            throw new NotSupportedException($"You can't call functions of a active Hub Client with active Connection.");
        }

        if (!Groups.Contains(SessionGroups.Listener))
        {
            logger.LogDebug($"Register Hub Client as Listener.");
            Groups.Add(SessionGroups.Listener);
        }

        return new Listener(this);
    }

    public Invoker registerInvoker()
    {
        if (IsActive)
        {
            throw new NotSupportedException($"You can't call functions of a active Hub Client with active Connection.");
        }

        if (!Groups.Contains(SessionGroups.Invoker))
        {
            logger.LogDebug($"Register Hub Client as Invoker.");
            Groups.Add(SessionGroups.Invoker);
        }

        return new Invoker(this);
    }

    public async Task StartAsync()
    {
        _ = Task.Run(async () =>
        {
            var session = new Session(this.Name, this.Groups.ToArray());
            logger.LogInformation($"Establishing connection to Hub as {session.Name} ({string.Join(", ", session.Groups)}).");

            await Connection.StartAsync();
            await Connection.SendAsync(Broadcasts.SessionConnect, session);

            await Task.Delay(-1);
        });

        while (!IsActive)
        {
            await Task.Delay(100);
        }
    }
}