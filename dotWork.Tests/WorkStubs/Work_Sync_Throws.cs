using dotWork.Tests.TestExceptions;

namespace dotWork.Tests.WorkStubs
{
    class Work_Sync_Throws
    {
        public void ExecuteIteration()
        {
            throw new IterationFinishedException();
        }
    }
}
