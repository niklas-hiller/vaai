using VAAI.Shared.Communication;

namespace VAAI.Library
{
    public class TaskQueue<T1, T2>
    {
        public Queue<T1> InputQueue = new();
        public Queue<Result<T2>> OutputQueue = new();
        public bool HasTasks { get => InputQueue.Count > 0; }
    }
}
