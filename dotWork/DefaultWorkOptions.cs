using System;
using System.Threading;

namespace dotWork
{
    /// <summary>
    /// <see cref="IsEnabled"/>: <i>true</i>.<br/>
    /// <see cref="DelayBetweenIterations"/>: <i>Infinite</i>.<br/>
    /// <see cref="StopOnException"/>: <i>false</i>.
    /// </summary>
    public class DefaultWorkOptions : IWorkOptions
    {
        public bool IsEnabled { get; set; } = true;

        public TimeSpan DelayBetweenIterations { get; set; } = Timeout.InfiniteTimeSpan;

        public int DelayBetweenIterationsInSeconds
        {
            get => (int)DelayBetweenIterations.TotalSeconds;
            set => DelayBetweenIterations = TimeSpan.FromSeconds(value);
        }

        public bool StopOnException { get; set; } = false;
    }
}
