using dotWork.Tests.Stubs;
using Xunit;

namespace dotWork.Tests.Works
{
    class Work_Ensure_Iteration_Lifetime
    {
        StubDependency? _prevDependency;
        public int ExecutedIterationsCount;

        public void ExecuteIteration(StubDependency dep)
        {
            if (_prevDependency == null)
            {
                _prevDependency = dep;
            }
            else
            {
                Assert.NotEqual(_prevDependency, dep);
            }
            ExecutedIterationsCount++;
        }
    }
}
