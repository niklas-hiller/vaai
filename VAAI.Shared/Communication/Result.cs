using System.Text.Json.Serialization;
using VAAI.Shared.Enums;

namespace VAAI.Shared.Communication;

public class Result<T>
{
    public EStatus Status { get; set; }
    public T Content { get; set; }

    [JsonConstructor]
    public Result(EStatus status, T content)
        => (Status, Content) = (status, content);
}
