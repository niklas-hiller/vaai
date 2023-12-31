﻿using VAAI.Shared.Communication;

namespace VAAI.Library;

public class TaskQueue<T1, T2>
{
    public readonly QueueObservable<T1> InputQueue = new();
    public readonly QueueObservable<Result<T2>> OutputQueue = new();

    private readonly List<Action<TaskQueue<T1, T2>>> InputObservers = new();
    private readonly List<Func<TaskQueue<T1, T2>, Task>> InputObserversAsync = new();
    private readonly List<Action<TaskQueue<T1, T2>>> OutputObservers = new();
    private readonly List<Func<TaskQueue<T1, T2>, Task>> OutputObserversAsync = new();
    public bool HasTasks { get => InputQueue.Count > 0; }
    public bool HasResults { get => OutputQueue.Count > 0; }

    private readonly SemaphoreSlim semaphoreSlim;

    public TaskQueue(int maximumConcurrent)
    {
        semaphoreSlim = new SemaphoreSlim(maximumConcurrent);

        InputQueue.OnEnqueue((queue) => InputObservers.ForEach((observer) => observer(this)));
        InputQueue.OnEnqueueAsync(async (queue) =>
        {
            foreach (var observer in InputObserversAsync)
            {
                await observer(this);
            }
        });

        OutputQueue.OnEnqueue((queue) => OutputObservers.ForEach((observer) => observer(this)));
        OutputQueue.OnEnqueueAsync(async (queue) =>
        {
            foreach (var observer in OutputObserversAsync)
            {
                await observer(this);
            }
        });
    }

    public async Task Next(Func<T1, Task<Result<T2>?>> task)
    {
        if (HasTasks)
        {
            await semaphoreSlim.WaitAsync();
            var result = await task(InputQueue.Dequeue());
            if (result != null)
            {
                OutputQueue.Enqueue(result);
            }
            semaphoreSlim.Release();
        }
    }

    public void OnInput(Action<TaskQueue<T1, T2>> action) 
        => InputObservers.Add(action);
    public void OnInputAsync(Func<TaskQueue<T1, T2>, Task> task) 
        => InputObserversAsync.Add(task);
    public void OnOutput(Action<TaskQueue<T1, T2>> action) 
        => OutputObservers.Add(action);
    public void OnOutputAsync(Func<TaskQueue<T1, T2>, Task> task) 
        => OutputObserversAsync.Add(task);
}
