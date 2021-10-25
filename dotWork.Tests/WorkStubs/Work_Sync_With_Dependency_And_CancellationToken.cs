using dotWork.Tests.Stubs;
using System.Threading;

namespace dotWork.Tests.WorkStubs
{
    class Work_Sync_With_Dependency_And_CancellationToken : WorkBase<DefaultWorkOptions>
    {
        public void ExecuteIteration(StubDependency dep, CancellationToken token)
        {
        }
    }
}
