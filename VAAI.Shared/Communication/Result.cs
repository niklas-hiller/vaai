using System.Text.Json.Serialization;
using VAAI.Shared.Enums;

namespace VAAI.Shared.Communication;

public class Result<T>
{
    public T Content { get; set; }
    public EStatus Status { get; set; }

    [JsonConstructor]
    public Result(EStatus status, T content)
    {
        Status = status;
        Content = content;
    }
}
