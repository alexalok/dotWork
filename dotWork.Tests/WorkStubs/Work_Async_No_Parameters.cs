using System.Threading.Tasks;

namespace dotWork.Tests
{
    class Work_Async_No_Parameters
    {
        public async Task ExecuteIteration()
        {
            await Task.Yield();
        }
    }
}
