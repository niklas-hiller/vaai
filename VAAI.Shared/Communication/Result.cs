using VAAI.Shared.Enums;

namespace VAAI.Shared.Communication;

public class Result<T>
{
    public T Content { get; set; }
    public EStatus Status { get; set; }

    public Result() { }

    public Result(EStatus status)
    {
        Status = status;
        Content = default;
    }

    public Result(EStatus status, T content)
    {
        Status = status;
        Content = content;
    }
}
