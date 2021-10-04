using System.Threading;
using System.Threading.Tasks;
using dotWork.Tests.Stubs;

namespace dotWork.Tests.WorkStubs
{
    class Work_Async_With_Dependency_And_CancellationToken
    {
        public async Task ExecuteIteration(StubDependency dep, CancellationToken token)
        {
            await Task.Yield();
        }
    }
}
