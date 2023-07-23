namespace VAAI.Shared.Communication;

public class Message
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public object Content { get; set; }

    public Message(object content)
    {
        Content = content;
    }

    public Message(Guid id, object content)
    {
        Id = id;
        Content = content;
    }
}
