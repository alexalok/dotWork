using dotWork.Tests.Stubs;
using Xunit;

namespace dotWork.Tests.WorkStubs
{
    class Work_Ensure_Iteration_Lifetime : WorkBase<DefaultWorkOptions>
    {
        StubDependency? _prevDependency;
        public int ExecutedIterationsCount;

        public void ExecuteIteration(StubDependency dep)
        {
            if (_prevDependency == null)
                _prevDependency = dep;
            else
            {
                Assert.NotEqual(_prevDependency, dep);
            }
            ExecutedIterationsCount++;
        }
    }
}
