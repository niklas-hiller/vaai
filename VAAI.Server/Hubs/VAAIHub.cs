using Microsoft.AspNetCore.SignalR;
using System.Net.Sockets;
using VAAI.Server.Filters;
using VAAI.Server.Services;
using VAAI.Shared.Communication;
using VAAI.Shared.Enums;

namespace VAAI.Server.Hubs;

internal class VAAIHub : Hub
{
    private readonly ILogger logger;
    private readonly ISessionService<VAAIHub> sessionService;

    public VAAIHub(ILogger<VAAIHub> logger, ISessionService<VAAIHub> sessionService)
    {
        this.logger = logger;
        this.sessionService = sessionService;
    }

    /// <summary>
    /// Establishes a session with a client, and informs all other clients about it.
    /// </summary>
    /// <param name="session"></param>
    /// <returns></returns>
    public async Task SessionConnect(Session session)
    {
        // Add the session to the group name.
        await sessionService.AddSessionAsync(Context.ConnectionId, session);
        logger.LogDebug($"{Context.ConnectionId} connected as {session}");

        await Clients.Caller.SendAsync(Broadcasts.SessionConnect);
    }

    /// <summary>
    /// Informs all TTS AI sessions to process a given text.
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    [GroupFilter(SessionGroups.Invoker)]
    public async Task<Guid> TextToSpeech(string text)
    {
        Message<string> message = new(text);

        Session session = sessionService.GetSession(Context.ConnectionId);
        logger.LogDebug($"{session} requests T2S: {text}");

        await Clients.Group(SessionGroups.TTS_AI).SendAsync(Broadcasts.TextToSpeech, message);
        return message.Id;
    }

    /// <summary>
    /// Informs all STT AI sessions to process a given audio.
    /// </summary>
    /// <param name="samples"></param>
    /// <returns></returns>
    [GroupFilter(SessionGroups.Invoker)]
    public async Task<Guid> SpeechToText(float[] samples)
    {
        Message<float[]> message = new(samples);

        Session session = sessionService.GetSession(Context.ConnectionId);
        logger.LogDebug($"{session} requests S2T: {samples.Length} Samples");

        await Clients.Group(SessionGroups.STT_AI).SendAsync(Broadcasts.SpeechToText, message);
        return message.Id;
    }

    /// <summary>
    /// Informs all LLM AI sessions to process a given text.
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    [GroupFilter(SessionGroups.Invoker)]
    public async Task<Guid> TextToText(string text)
    {
        Message<string> message = new(text);

        Session session = sessionService.GetSession(Context.ConnectionId);
        logger.LogDebug($"{session} requests T2T: {text}");

        await Clients.Group(SessionGroups.LLM_AI).SendAsync(Broadcasts.TextToText, message);
        return message.Id;
    }

    /// <summary>
    /// Informs all Listener sessions of the result of a TTS AI session.
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    [GroupFilter(SessionGroups.TTS_AI)]
    public async Task TextToSpeechResult(Message<Result<float[]>> message)
    {
        Session session = sessionService.GetSession(Context.ConnectionId);
        switch (message.Content.Status)
        {
            case EStatus.DROPPED:
                logger.LogDebug($"{session} dropped T2S ({message.Id})");
                break;
            case EStatus.WAIT_FOR_MORE:
                logger.LogDebug($"{session} waits for more data to T2S ({message.Id})");
                break;
            case EStatus.DONE:
                logger.LogInformation($"{session} finished T2S ({message.Id})");
                break;
        }

        await Clients.Group(SessionGroups.Listener).SendAsync(Broadcasts.TextToSpeechResult, message);
    }

    /// <summary>
    /// Informs all Listener sessions of the result of a STT AI session.
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    [GroupFilter(SessionGroups.STT_AI)]
    public async Task SpeechToTextResult(Message<Result<string>> message)
    {
        Session session = sessionService.GetSession(Context.ConnectionId);
        switch (message.Content.Status)
        {
            case EStatus.DROPPED:
                logger.LogDebug($"{session} dropped S2T ({message.Id})");
                break;
            case EStatus.WAIT_FOR_MORE:
                logger.LogDebug($"{session} waits for more data to S2T ({message.Id})");
                break;
            case EStatus.DONE:
                logger.LogInformation($"{session} finished S2T ({message.Id}): {message.Content.Content}");
                break;
        }

        await Clients.Group(SessionGroups.Listener).SendAsync(Broadcasts.SpeechToTextResult, message);
    }

    /// <summary>
    /// Informs all Listener sessions of the result of a LLM AI session.
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    [GroupFilter(SessionGroups.LLM_AI)]
    public async Task TextToTextResult(Message<Result<string>> message)
    {
        Session session = sessionService.GetSession(Context.ConnectionId);
        switch (message.Content.Status)
        {
            case EStatus.DROPPED:
                logger.LogDebug($"{session} dropped T2T ({message.Id})");
                break;
            case EStatus.WAIT_FOR_MORE:
                logger.LogDebug($"{session} waits for more data to T2T ({message.Id})");
                break;
            case EStatus.DONE:
                logger.LogInformation($"{session} finished T2T ({message.Id})");
                break;
        }

        await Clients.Group(SessionGroups.Listener).SendAsync(Broadcasts.TextToTextResult, message);
    }
}
