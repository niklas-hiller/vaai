using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VAAI.Discord.Models;
using VAAI.Discord.Services;

namespace VAAI.Discord.Handler;

internal class CommandHandler : ICommandHandler
{
    private readonly DiscordSocketClient client;
    private readonly ILogger logger;
    private readonly ILocalizationService localization;
    private readonly IVoiceService voice;

    public CommandHandler(ILogger<CommandHandler> logger, DiscordSocketClient client, ILocalizationService localization, IVoiceService voice)
    {
        this.logger = logger;
        this.client = client;
        this.localization = localization;
        this.voice = voice;
    }

    public async Task Initialize()
    {
        #region Load commands.json
        List<Command> commands =
            JsonConvert.DeserializeObject<Dictionary<string, Command>>(File.ReadAllText("commands.json"))?.Values.ToList()
            ?? new List<Command>();
        #endregion

        #region Construct Commands
        ApplicationCommandProperties[] applicationCommandProperties = commands.Select(command =>
        {
            logger.LogInformation($"Loading command {localization.GetLocalizedValue(command.Name)}...");
            try
            {
                SlashCommandBuilder commandBuilder = new SlashCommandBuilder();
                commandBuilder.WithName(localization.GetLocalizedValue(command.Name));
                commandBuilder.WithNameLocalizations(localization.GetLocalizedValues(command.Name));
                commandBuilder.WithDescription(localization.GetLocalizedValue(command.Description));
                commandBuilder.WithDescriptionLocalizations(localization.GetLocalizedValues(command.Description));
                command.Options.ForEach(option =>
                {
                    SlashCommandOptionBuilder optionBuilder = new SlashCommandOptionBuilder();
                    optionBuilder.WithName(localization.GetLocalizedValue(option.Name));
                    optionBuilder.WithNameLocalizations(localization.GetLocalizedValues(option.Name));
                    optionBuilder.WithDescription(localization.GetLocalizedValue(option.Description));
                    optionBuilder.WithDescriptionLocalizations(localization.GetLocalizedValues(option.Description));
                    optionBuilder.WithRequired(option.Required);
                    optionBuilder.WithType(option.OptionType());
                    option.Choices.ForEach(choice =>
                    {
                        optionBuilder.AddChoice(
                            name: localization.GetLocalizedValue(choice.Name),
                            value: choice.Value,
                            nameLocalizations: localization.GetLocalizedValues(choice.Name));
                    });
                    commandBuilder.AddOption(optionBuilder);
                });
                logger.LogInformation($"...Successful!");
                return commandBuilder.Build();
            }
            catch (Exception)
            {
                logger.LogError($"...Failed!");
                throw;
            }
        }).Where(property => property != null).ToArray();
        #endregion

        #region Send Commands to Discord
        logger.LogInformation($"Sending commands to Discord...");
        try
        {
            await client.BulkOverwriteGlobalApplicationCommandsAsync(applicationCommandProperties);
            logger.LogInformation("...Successful!");
        }
        catch (Exception)
        {
            logger.LogError($"...Failed!");
            throw;
        }
        #endregion

        client.SlashCommandExecuted += Handle;
    }

    private async Task Handle(SocketSlashCommand command)
    {
        logger.LogInformation($"User executed {command.Data.Name}");
        switch (command.Data.Name)
        {
            case "connect":
                await HandleConnectCommand(command);
                break;
            case "disconnect":
                await HandleDisconnectCommand(command);
                break;
        }
    }

    private async Task HandleConnectCommand(SocketSlashCommand command)
    {
        var channel = (command.User as IGuildUser)?.VoiceChannel;
        if (channel == null)
        {
            await command.RespondAsync("You must be in a voice channel.");
            return;
        }
        voice.Connect(channel);
        await command.RespondAsync($"Connected to {channel.Name}.");
    }

    private async Task HandleDisconnectCommand(SocketSlashCommand command)
    {
        var channel = (command.User as IGuildUser)?.VoiceChannel;
        if (channel == null)
        {
            await command.RespondAsync("You must be in a voice channel.");
            return;
        }

        voice.Disconnect(channel);
        await command.RespondAsync($"Disconnected from voice.");
    }
}
