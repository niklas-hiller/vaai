using Microsoft.AspNetCore.SignalR;

namespace VAAI.Server.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class GroupFilterAttribute : Attribute, IHubFilter
{
    public readonly string[] Groups;

    public GroupFilterAttribute(params string[] groups)
    {
        this.Groups = groups;
    }
}