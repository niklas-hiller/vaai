using Microsoft.AspNetCore.SignalR;
using VAAI.Shared.Communication;

namespace VAAI.Server.Services;

internal interface ISessionService<T> where T : Hub
{
    Task AddSessionAsync(string connectionId, Session session);
    Task RemoveSessionAsync(string connectionId);

    bool InGroup(string connectionId, string groupName);
    bool InGroupAny(string connectionId, string[] groupNames);
    bool InGroupAll(string connectionId, string[] groupNames);

    Session? TryGetSession(string connectionId);
    Session GetSession(string connectionId);
}
