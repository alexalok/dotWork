using System;
using System.Threading;
using System.Threading.Tasks;

namespace dotWork.Example
{
    public class ExampleWork : WorkBase<DefaultWorkOptions>
    {
        readonly SingletonService _singletonService;

        public ExampleWork(SingletonService singletonService)
        {
            _singletonService = singletonService;
        }

        public async Task ExecuteIteration(ScopedService scopedService, CancellationToken ct)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), ct);
            Console.WriteLine("Work iteration finished!");
        }
    }
}
