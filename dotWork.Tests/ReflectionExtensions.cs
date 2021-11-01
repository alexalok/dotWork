using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace dotWork.Tests
{
    static class ReflectionExtensions
    {
        public static Task GetExecutingTask(this BackgroundService workHost)
        {
            var fieldInfo = typeof(BackgroundService).GetField("_executingTask", BindingFlags.Instance | BindingFlags.NonPublic);
            return (Task) fieldInfo!.GetValue(workHost)!;
        }
    }
}
