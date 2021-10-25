using dotWork.Tests.Stubs;
using System.Threading;
using System.Threading.Tasks;

namespace dotWork.Tests.WorkStubs
{
    class Work_Async_With_Dependency_And_CancellationToken : WorkBase<DefaultWorkOptions>
    {
        public async Task ExecuteIteration(StubDependency dep, CancellationToken token)
        {
            await Task.Yield();
        }
    }
}
