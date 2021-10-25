namespace dotWork.Tests.WorkStubs
{
    class Work_Sync_With_Counter2 : WorkBase<DefaultWorkOptions>
    {
        public int ExecutedIterationsCount;

        public void ExecuteIteration()
        {
            ExecutedIterationsCount++;
        }
    }
}
