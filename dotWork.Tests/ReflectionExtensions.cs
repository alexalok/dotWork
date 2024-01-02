using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace dotWork.Tests
{
    static class ReflectionExtensions
    {
        public static Task GetExecutingTask(this BackgroundService workHost)
        {
            return workHost.ExecuteTask!;
        }
    }
}
