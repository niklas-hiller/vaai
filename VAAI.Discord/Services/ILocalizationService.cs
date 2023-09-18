namespace VAAI.Discord.Services;

internal interface ILocalizationService
{
    public Dictionary<string, string> GetLocalizedValues(string key);
    public string GetLocalizedValue(string key, string locale);
    public string GetLocalizedValue(string key);
}
