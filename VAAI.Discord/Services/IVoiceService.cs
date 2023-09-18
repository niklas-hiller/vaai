using Discord;

namespace VAAI.Discord.Services;

internal interface IVoiceService
{
    public void Connect(IVoiceChannel vc);
    public void Disconnect(IVoiceChannel vc);
}
