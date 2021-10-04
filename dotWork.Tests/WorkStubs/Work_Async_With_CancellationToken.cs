using System.Threading;
using System.Threading.Tasks;

namespace dotWork.Tests.WorkStubs
{
    class Work_Async_With_CancellationToken
    {
        public async Task ExecuteIteration(CancellationToken token)
        {
            await Task.Yield();
        }
    }
}
