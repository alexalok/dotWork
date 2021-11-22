using System.Threading.Tasks;

namespace dotWork.Tests.WorkStubs
{
    public class Work_With_ValueTask : WorkBase<DefaultWorkOptions>
    {
        public async ValueTask ExecuteIteration()
        {
        }
    }
}