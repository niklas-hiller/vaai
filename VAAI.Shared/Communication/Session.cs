using System.Text.Json.Serialization;

namespace VAAI.Shared.Communication;

public class Session
{
    public string Name { get; set; } = "";
    public string[] Groups { get; set; } = Array.Empty<string>();

    [JsonConstructor]
    public Session(string name, string[] groups)
        => (Name, Groups) = (name, groups);

    public override string ToString()
        => $"{Name} ({string.Join(", ", Groups)})";
}
