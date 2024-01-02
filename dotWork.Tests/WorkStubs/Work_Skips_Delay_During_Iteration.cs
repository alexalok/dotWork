using dotWork.Tests.Stubs;
using System.Threading.Tasks;
using Xunit;

namespace dotWork.Tests.WorkStubs
{
    class Work_Skips_Delay_During_Iteration : WorkBase<DefaultWorkOptions>
    {
        public int ExecutedIterationsCount;

        public async void ExecuteIteration()
        {
            ExecutedIterationsCount++;
            SkipDelayOnce();
            await Task.Yield();
        }
    }
}
