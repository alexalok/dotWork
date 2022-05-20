using System.Threading.Tasks;

namespace dotWork.Tests
{
    public interface IAsyncWork : IWork<DefaultWorkOptions>
    {
        Task ExecuteIteration();
    }
}