﻿using Microsoft.AspNetCore.SignalR;
using VAAI.Server.Hubs;
using VAAI.Server.Services;
using VAAI.Shared.Enums;

namespace VAAI.Server.Filters;

internal class GroupFilter<T> : IHubFilter where T : Hub
{
    private readonly ILogger logger;
    private readonly ISessionService<T> sessionService;

    public GroupFilter(ILogger<VAAIHub> logger, ISessionService<T> sessionService)
    {
        this.logger = logger;
        this.sessionService = sessionService;
    }

    public async ValueTask<object?> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next)
    {
        var groupFilter = Attribute.GetCustomAttribute(invocationContext.HubMethod, typeof(GroupFilterAttribute));
        if (groupFilter == null)
        {
            return await next(invocationContext);
        }
        var groups = ((GroupFilterAttribute)groupFilter).Groups;

        var connectionId = invocationContext.Context.ConnectionId;
        var session = sessionService.TryGetSession(connectionId);
        if (session == null)
        {
            string message = $"[{EServerError.NO_SESSION}] No active session for the connection ({connectionId}) detected.";
            logger.LogError(message);
            throw new HubException(message);
        }
        if (!sessionService.InGroupAny(invocationContext.Context.ConnectionId, groups))
        {
            string message = $"[{EServerError.INVALID_GROUP}] {session.Name} is not part of permitted groups: {groups}";
            logger.LogError(message);
            throw new HubException(message);
        }

        return await next(invocationContext);
    }

    public Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
    {
        logger.LogInformation($"New connection established to {context.Context.ConnectionId}.");
        return next(context);
    }

    public Task OnDisconnectedAsync(HubLifetimeContext context, Exception? exception, Func<HubLifetimeContext, Exception?, Task> next)
    {
        logger.LogInformation($"Connection abolished to {context.Context.ConnectionId}.");
        if (exception != null)
        {
            logger.LogError(exception.Message);
        }
        sessionService.RemoveSessionAsync(context.Context.ConnectionId);
        return next(context, exception);
    }
}