using dotWork.Tests.TestExceptions;

namespace dotWork.Tests.WorkStubs
{
    class Work_Sync_Throws : WorkBase<DefaultWorkOptions>
    {
        public void ExecuteIteration()
        {
            throw new IterationFinishedException();
        }
    }
}
