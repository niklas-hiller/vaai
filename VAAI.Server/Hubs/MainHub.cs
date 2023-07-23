using Microsoft.AspNetCore.SignalR;
using System.Net.Sockets;
using VAAI.Server.Filters;
using VAAI.Shared.Communication;
using VAAI.Shared.Enums;

namespace VAAI.Server.Hubs;

public class MainHub : Hub
{
    private readonly ILogger logger;

    public MainHub(ILogger<MainHub> logger)
    {
        this.logger = logger;
    }

    /// <summary>
    /// Establishes a session with a client. 
    /// Sends error if client tries to establish session with unknown group.
    /// </summary>
    /// <param name="session"></param>
    /// <returns></returns>
    public async Task SessionConnect(Session session)
    {
        // Check if session group exists
        if (!SessionGroups.Contains(session.GroupName))
        {
            logger.LogError($"{session.Name} tried to connect as unknown group: {session.GroupName}");
            await Clients.Caller.SendAsync(Broadcasts.ServerError, EServerError.UNKNOWN_GROUP);
            return;
        }

        // Add the session to the group name.
        await Groups.AddToGroupAsync(Context.ConnectionId, session.GroupName);
        logger.LogInformation($"{session.Name} connected as {session.GroupName}");

        await Clients.Caller.SendAsync(Broadcasts.SessionConnect);
        await Clients.Others.SendAsync(Broadcasts.OtherSessionConnect, session.GroupName);
        // await Clients.OthersInGroup(session.GroupName).SendAsync(ServerBroadcasts.SessionConnected, session);
        // await Clients.Group(SessionGroups.Listener).SendAsync(ServerBroadcasts.SendTicket, ticket);
    }

    /// <summary>
    /// Informs all LLM AI sessions to process a given text.
    /// </summary>
    /// <param name="session"></param>
    /// <param name="text"></param>
    /// <returns></returns>
    [GroupFilter(SessionGroups.Invoker, SessionGroups.STT_AI, SessionGroups.LLM_AI)]
    public async Task TextToText(Session session, string text)
    {
        logger.LogInformation($"{session.Name} ({session.GroupName}) requests T2T: {text}");

        await Clients.Group(SessionGroups.LLM_AI).SendAsync(Broadcasts.TextToText, text);
    }

    /// <summary>
    /// Informs all TTS AI sessions to process a given text.
    /// </summary>
    /// <param name="session"></param>
    /// <param name="text"></param>
    /// <returns></returns>
    [GroupFilter(SessionGroups.Invoker, SessionGroups.STT_AI, SessionGroups.LLM_AI)]
    public async Task TextToSpeech(Session session, string text)
    {
        logger.LogInformation($"{session.Name} ({session.GroupName}) requests T2S: {text}");

        await Clients.Group(SessionGroups.TTS_AI).SendAsync(Broadcasts.TextToSpeech, text);
    }

    /// <summary>
    /// Informs all STT AI sessions to process a given audio.
    /// </summary>
    /// <param name="session"></param>
    /// <param name="samples"></param>
    /// <returns></returns>
    [GroupFilter(SessionGroups.Invoker, SessionGroups.TTS_AI)]
    public async Task SpeechToText(Session session, float[] samples)
    {
        logger.LogInformation($"{session.Name} ({session.GroupName}) requests S2T: {samples.Length} Samples");

        await Clients.Group(SessionGroups.STT_AI).SendAsync(Broadcasts.SpeechToText, samples);
    }

    /// <summary>
    /// Informs all Listener sessions of the result of a LLM AI session.
    /// </summary>
    /// <param name="session"></param>
    /// <param name="text"></param>
    /// <returns></returns>
    [GroupFilter(SessionGroups.LLM_AI)]
    public async Task TextToTextFinish(Session session, string text)
    {
        logger.LogInformation($"{session.Name} ({session.GroupName}) finished T2T: {text}");

        await Clients.Group(SessionGroups.Listener).SendAsync(Broadcasts.TextToTextResult, text);
    }

    /// <summary>
    /// Informs all Listener sessions of the result of a TTS AI session.
    /// </summary>
    /// <param name="session"></param>
    /// <param name="text"></param>
    /// <returns></returns>
    [GroupFilter(SessionGroups.TTS_AI)]
    public async Task TextToSpeechFinish(Session session, float[] samples)
    {
        logger.LogInformation($"{session.Name} ({session.GroupName}) finished T2S: {samples.Length} Samples");

        await Clients.Group(SessionGroups.Listener).SendAsync(Broadcasts.TextToSpeechResult, samples);
    }

    /// <summary>
    /// Informs all Listener sessions of the result of a STT AI session.
    /// </summary>
    /// <param name="session"></param>
    /// <param name="text"></param>
    /// <returns></returns>
    [GroupFilter(SessionGroups.STT_AI)]
    public async Task SpeechToTextFinish(Session session, string text)
    {
        logger.LogInformation($"{session.Name} ({session.GroupName}) finished S2T: {text}");

        await Clients.Group(SessionGroups.Listener).SendAsync(Broadcasts.SpeechToTextResult, text);
    }
}
