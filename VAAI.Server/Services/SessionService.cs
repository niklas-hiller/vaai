using Microsoft.AspNetCore.SignalR;
using VAAI.Shared.Communication;
using VAAI.Shared.Enums;

namespace VAAI.Server.Services;

public class SessionService<T> : ISessionService<T> where T : Hub
{
    private readonly ILogger logger;
    private readonly IHubContext<T> hubContext;

    private readonly Dictionary<string, Session> Sessions = new();

    public SessionService(ILogger<SessionService<T>> logger, IHubContext<T> hubContext)
    {
        this.logger = logger;
        this.hubContext = hubContext;
    }

    public async Task AddSessionAsync(string connectionId, Session session)
    {
        if (Sessions.ContainsKey(connectionId))
        {
            string message = $"[{EServerError.EXISTING_SESSION}] {session.Name} has already a active sessions.";
            logger.LogError(message);
            throw new HubException(message);
        }
        if (!session.Groups.All(SessionGroups.Contains))
        {
            string message = $"[{EServerError.UNKNOWN_GROUP}] {session.Name} contains unknown groups.";
            logger.LogError(message);
            throw new HubException(message);
        }

        foreach (var group in session.Groups)
        {
            await hubContext.Groups.AddToGroupAsync(connectionId, group);
        }
        Sessions[connectionId] = session;

        logger.LogInformation($"Added session for {connectionId} as {session.Name} ({string.Join(", ", session.Groups)})");
    }

    public async Task RemoveSessionAsync(string connectionId)
    {
        if (!Sessions.ContainsKey(connectionId))
        {
            string message = $"[{EServerError.NO_SESSION}] {connectionId} has no active sessions.";
            logger.LogError(message);
            throw new HubException(message);
        }

        Session session = Sessions[connectionId];
        foreach (var group in session.Groups)
        {
            await hubContext.Groups.RemoveFromGroupAsync(connectionId, group);
        }
        Sessions.Remove(connectionId);

        logger.LogInformation($"Removed session of {connectionId} as {session.Name} ({string.Join(", ", session.Groups)})");
    }

    public bool InGroup(string connectionId, string groupName)
    {
        return Sessions[connectionId].Groups.Any((group) => group == groupName);
    }

    public bool InGroupAny(string connectionId, string[] groupNames)
    {
        if (groupNames.Length == 0)
        {
            return true;
        }
        return groupNames.Any((groupName) => Sessions[connectionId].Groups.Any((group) => group == groupName));
    }

    public bool InGroupAll(string connectionId, string[] groupNames)
    {
        if (groupNames.Length == 0)
        {
            return true;
        }
        return groupNames.All((groupName) => Sessions[connectionId].Groups.Any((group) => group == groupName));
    }

    public Session? TryGetSession(string connectionId)
    {
        return Sessions.ContainsKey(connectionId) ? Sessions[connectionId] : null;
    }

    public Session GetSession(string connectionId)
    {
        return Sessions[connectionId];
    }
}
