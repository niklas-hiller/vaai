using VAAI.Shared.Communication;

namespace VAAI.Library;

public class MessageQueue<T1, T2>
{
    public TaskQueue<T1, T2> Tasks = new(1);
    private readonly QueueObservable<Guid> Queue = new();

    public void Enqueue(Message<T1> message)
    {
        Queue.Enqueue(message.Id);
        Tasks.InputQueue.Enqueue(message.Content);
    }

    public Message<Result<T2>> Dequeue()
    {
        return new Message<Result<T2>>(Queue.Dequeue(), Tasks.OutputQueue.Dequeue());
    }
}
