using System.Threading;

namespace dotWork.Tests.WorkStubs
{
    class Work_Sync_With_CancellationToken : WorkBase<DefaultWorkOptions>
    {
        public void ExecuteIteration(CancellationToken token)
        {
        }
    }
}
