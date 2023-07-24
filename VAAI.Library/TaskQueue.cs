using System.Threading.Tasks;

namespace VAAI.Library
{
    public class TaskQueue<T1, T2>
    {
        public Queue<T1> InputQueue = new Queue<T1>();
        public Queue<T2> OutputQueue = new Queue<T2>();
        public bool HasTasks { get => InputQueue.Count > 0; }
    }
}
