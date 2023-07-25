using System.Text.Json.Serialization;

namespace VAAI.Shared.Communication;

public class Message<T>
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public T Content { get; set; }

    public Message(T content)
    {
        Content = content;
    }

    [JsonConstructor]
    public Message(Guid id, T content)
    {
        Id = id;
        Content = content;
    }
}
