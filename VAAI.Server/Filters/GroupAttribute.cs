using Microsoft.AspNetCore.SignalR;
using System.Reflection;
using VAAI.Shared.Communication;

namespace VAAI.Server.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class GroupFilterAttribute : Attribute, IHubFilter
{
    private readonly string[] Groups;

    public GroupFilterAttribute(params string[] groups)
    {
        this.Groups = groups;
    }

    public async ValueTask<object> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object>> next)
    {
        if (invocationContext.HubMethodArguments.Count == 0)
        {
            throw new InvalidFilterCriteriaException("Can't use group filter on hub methods without session.");
        }
        var expectedSession = invocationContext.HubMethodArguments[0];
        if (expectedSession == null)
        {
            throw new InvalidFilterCriteriaException("Can't use group filter on hub methods with null values on session attribute.");
        }
        if (expectedSession.GetType() == typeof(Session))
        {
            throw new InvalidFilterCriteriaException("Must use session as first argument.");
        }
        Session session = (Session)expectedSession;
        if (!this.Groups.Contains(session.GroupName))
        {
            throw new Exception($"{session.Name} ({session.GroupName}) is not part of permitted groups: {this.Groups}");
        }

        return await next(invocationContext);
    }

    // Optional method
    //public Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
    //{
    //    return next(context);
    //}

    // Optional method
    //public Task OnDisconnectedAsync(HubLifetimeContext context, Exception exception, Func<HubLifetimeContext, Exception, Task> next)
    //{
    //    return next(context, exception);
    //}
}