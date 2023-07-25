namespace VAAI.Library;

public class QueueObservable<T> : Queue<T>
{
    private readonly List<Action<QueueObservable<T>, T>> DequeueObservers = new();
    private readonly List<Func<QueueObservable<T>, T, Task>> DequeueObserversAsync = new();
    private readonly List<Action<QueueObservable<T>>> EnqueueObservers = new();
    private readonly List<Func<QueueObservable<T>, Task>> EnqueueObserversAsync = new();

    public new void Enqueue(T item)
    {
        base.Enqueue(item);
        Task.Run(() =>
        {
            EnqueueObservers.ForEach((observer) => observer(this));
            EnqueueObserversAsync.ForEach(async (observer) => await observer(this));
        });
    }

    public new T Dequeue()
    {
        T item = base.Dequeue();
        Task.Run(() =>
        {
            DequeueObservers.ForEach((observer) => observer(this, item));
            DequeueObserversAsync.ForEach(async (observer) => await observer(this, item));
        });
        return item;
    }

    public void OnEnqueue(Action<QueueObservable<T>> action) => EnqueueObservers.Add(action);
    public void OnDequeue(Action<QueueObservable<T>, T> action) => DequeueObservers.Add(action);
    public void OnEnqueueAsync(Func<QueueObservable<T>, Task> task) => EnqueueObserversAsync.Add(task);
    public void OnDequeueAsync(Func<QueueObservable<T>, T, Task> task) => DequeueObserversAsync.Add(task);
}
