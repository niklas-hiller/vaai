using VAAI.Shared.Communication;

namespace VAAI.Library
{
    public class TaskQueue<T1, T2>
    {
        public Queue<T1> InputQueue = new Queue<T1>();
        public Queue<Result<T2>> OutputQueue = new Queue<Result<T2>>();
        public bool HasTasks { get => InputQueue.Count > 0; }
    }
}
