using System.Threading.Tasks;

namespace dotWork.Tests.WorkStubs
{
    class Work_Async_No_Parameters : WorkBase<DefaultWorkOptions>
    {
        public async Task ExecuteIteration()
        {
            await Task.Yield();
        }
    }
}
