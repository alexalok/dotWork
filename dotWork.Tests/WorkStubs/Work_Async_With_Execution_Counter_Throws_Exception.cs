using dotWork.Tests.TestExceptions;
using System.Threading.Tasks;

namespace dotWork.Tests.WorkStubs
{
    class Work_Async_With_Execution_Counter_Throws_Exception : WorkBase<DefaultWorkOptions>
    {
        public int ExecutedIterationsCount;

        public async Task ExecuteIteration()
        {
            await Task.Yield();
            ExecutedIterationsCount++;
            throw new IterationFinishedException();
        }
    }
}
