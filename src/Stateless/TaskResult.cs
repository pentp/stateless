using System.Threading.Tasks;

namespace Stateless
{
    internal static class TaskResult
    {
        internal static Task Done => Task.CompletedTask;
    }
}