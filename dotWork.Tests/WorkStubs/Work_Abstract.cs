using System.Threading.Tasks;

namespace dotWork.Tests.WorkStubs
{
    public abstract class Work_Abstract<TWorkOptions> : WorkBase<TWorkOptions> 
        where TWorkOptions: class, IWorkOptions
    {
    }
}