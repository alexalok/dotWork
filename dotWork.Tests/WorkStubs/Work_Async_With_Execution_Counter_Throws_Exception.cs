using System.Threading.Tasks;
using dotWork.Tests.TestExceptions;

namespace dotWork.Tests.WorkStubs
{
    class Work_Async_With_Execution_Counter_Throws_Exception
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
