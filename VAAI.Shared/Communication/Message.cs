namespace VAAI.Shared.Communication;

public class Message<T>
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public T Content { get; set; }

#pragma warning disable CS8618 // Ein Non-Nullable-Feld muss beim Beenden des Konstruktors einen Wert ungleich NULL enthalten. Erwägen Sie die Deklaration als Nullable.
    public Message() { }
#pragma warning restore CS8618 // Ein Non-Nullable-Feld muss beim Beenden des Konstruktors einen Wert ungleich NULL enthalten. Erwägen Sie die Deklaration als Nullable.

    public Message(T content)
    {
        Content = content;
    }

    public Message(Guid id, T content)
    {
        Id = id;
        Content = content;
    }
}
