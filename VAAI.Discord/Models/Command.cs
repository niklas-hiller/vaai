using Discord;

namespace VAAI.Discord.Models;

internal class CommandChoices
{
    public string Name { get; set; } = "";
    public string Value { get; set; } = "";
}

internal class CommandOptions
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public bool Required { get; set; } = false;
    public string Type { get; set; } = "string";
    public List<CommandChoices> Choices { get; set; } = new();

    public ApplicationCommandOptionType OptionType()
        => Enum.Parse<ApplicationCommandOptionType>(Type, true);
}

internal class Command
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public List<CommandOptions> Options { get; set; } = new();
}
