using Microsoft.AspNetCore.SignalR;
using System.Net.Sockets;
using VAAI.Server.Filters;
using VAAI.Server.Services;
using VAAI.Shared.Communication;

namespace VAAI.Server.Hubs;

public class VAAIHub : Hub
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
        logger.LogInformation($"{session.Name} connected as {string.Join(", ", session.Groups)}");

        await Clients.Caller.SendAsync(Broadcasts.SessionConnect);
        await Clients.Others.SendAsync(Broadcasts.OtherSessionConnect, session);
    }

    /// <summary>
    /// Informs all TTS AI sessions to process a given text.
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    [GroupFilter(SessionGroups.Invoker)]
    public async Task<Guid> TextToSpeech(string text)
    {
        Message<string> message = new Message<string>(text);

        Session session = sessionService.GetSession(Context.ConnectionId);
        logger.LogInformation($"{session.Name} ({string.Join(", ", session.Groups)}) requests T2S: {text}");

        await Clients.Group(SessionGroups.TTS_AI).SendAsync(Broadcasts.TextToSpeech, text);
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
        Message<float[]> message = new Message<float[]>(samples);

        Session session = sessionService.GetSession(Context.ConnectionId);
        logger.LogInformation($"{session.Name} ({string.Join(", ", session.Groups)}) requests S2T: {samples.Length} Samples");

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
        Message<string> message = new Message<string>(text);

        Session session = sessionService.GetSession(Context.ConnectionId);
        logger.LogInformation($"{session.Name} ({string.Join(", ", session.Groups)}) requests T2T: {text}");

        await Clients.Group(SessionGroups.LLM_AI).SendAsync(Broadcasts.TextToText, text);
        return message.Id;
    }

    /// <summary>
    /// Informs all Listener sessions of the result of a TTS AI session.
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    [GroupFilter(SessionGroups.TTS_AI)]
    public async Task TextToSpeechResult(Message<float[]> message)
    {
        Session session = sessionService.GetSession(Context.ConnectionId);
        logger.LogInformation($"{session.Name} ({string.Join(", ", session.Groups)}) finished T2S: {message.Id}");

        await Clients.Group(SessionGroups.Listener).SendAsync(Broadcasts.TextToSpeechResult, message);
    }

    /// <summary>
    /// Informs all Listener sessions of the result of a STT AI session.
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    [GroupFilter(SessionGroups.STT_AI)]
    public async Task SpeechToTextResult(Message<string> message)
    {
        Session session = sessionService.GetSession(Context.ConnectionId);
        logger.LogInformation($"{session.Name} ({string.Join(", ", session.Groups)}) finished S2T: {message.Id}");

        await Clients.Group(SessionGroups.Listener).SendAsync(Broadcasts.SpeechToTextResult, message);
    }

    /// <summary>
    /// Informs all Listener sessions of the result of a LLM AI session.
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    [GroupFilter(SessionGroups.LLM_AI)]
    public async Task TextToTextResult(Message<string> message)
    {
        Session session = sessionService.GetSession(Context.ConnectionId);
        logger.LogInformation($"{session.Name} ({string.Join(", ", session.Groups)}) finished T2T: {message.Id}");

        await Clients.Group(SessionGroups.Listener).SendAsync(Broadcasts.TextToTextResult, message);
    }
}
