using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System.Linq;
using VAAI.Shared.Communication;
using VAAI.Shared.Enums;

namespace VAAI.Library;

public class HubClient
{
    public HubConnection Connection { get; set; }
    private string Name { get; set; }
    private List<string> Groups { get; set; }
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
            logger.LogInformation("Successfully connected to Hub.");
            IsActive = true;
        });
    }

    public TaskQueue<T1, T2> RegisterProcessor<T1, T2>(EProcessor processor)
    {
        if (IsActive)
        {
            throw new NotSupportedException($"You can't call functions of a active Hub Client with active Connection.");
        }

        var sessionGroup = processor.ToGroup();
        if (Groups.Contains(sessionGroup))
        {
            throw new NotSupportedException($"You can't register the {sessionGroup} multiple times.");
        }
        logger.LogDebug($"Register Hub Client as {sessionGroup}.");
        Groups.Add(sessionGroup);

        var messageQueue = new MessageQueue<T1, T2>();
        Connection.On<Message<T1>>(processor.ToIngoingBroadcast(), messageQueue.Enqueue);
        messageQueue.Tasks.OnOutputAsync(async (queue) =>
        {
            if (queue.HasResults)
            {
                await Connection.SendAsync(processor.ToOutgoingBroadcast(), messageQueue.Dequeue());
            }
        });

        return messageQueue.Tasks;
    }

    public Listener RegisterListener()
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

    public Invoker RegisterInvoker()
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
            await Task.Delay(10);
        }
    }
}