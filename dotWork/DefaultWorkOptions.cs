using System;
using System.Threading;

namespace dotWork
{
    /// <summary>
    /// <see cref="DelayBetweenIterations"/>: <i>Infinite</i>.<br/>
    /// <see cref="StopOnException"/>: <i>false</i>.
    /// </summary>
    public class DefaultWorkOptions : IWorkOptions
    {
        public TimeSpan DelayBetweenIterations { get; set; } = Timeout.InfiniteTimeSpan;

        public bool StopOnException { get; set; } = false;
    }
}
