using Microsoft.AspNetCore.SignalR;

namespace VAAI.Server.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
internal class GroupFilterAttribute : Attribute, IHubFilter
{
    public readonly string[] Groups;

    public GroupFilterAttribute(params string[] groups)
        => Groups = groups;
}