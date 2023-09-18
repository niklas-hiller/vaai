using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace VAAI.Discord.Services;

internal class LocalizationService : ILocalizationService
{
    private readonly ILogger logger;

    public string defaultLanguage = "en-US";
    private Dictionary<string, Dictionary<string, string>> localizations =
        JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(File.ReadAllText("localization.json"))
        ?? new Dictionary<string, Dictionary<string, string>>();

    public LocalizationService(ILogger<LocalizationService> logger)
    {
        this.logger = logger;
    }

    public Dictionary<string, string> GetLocalizedValues(string key)
    {
        return localizations.ContainsKey(key)
            ? localizations[key]
            : new() { { defaultLanguage, key } };
    }

    public string GetLocalizedValue(string key, string locale)
    {
        return GetLocalizedValues(key).ContainsKey(locale)
            ? GetLocalizedValues(key)[locale]
            : GetLocalizedValues(key)[defaultLanguage];
    }

    public string GetLocalizedValue(string key)
        => GetLocalizedValues(key)[defaultLanguage];
}
