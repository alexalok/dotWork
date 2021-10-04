using System;

namespace dotWork
{
    /// <summary>
    /// <see cref="DelayBetweenIterations"/>: <i>Infinite</i>.<br/>
    /// <see cref="StopOnException"/>: <i>false</i>.
    /// </summary>
    public class DefaultWorkOptions : IWorkOptions
    {
        public TimeSpan DelayBetweenIterations { get; set; } = TimeSpan.FromMilliseconds(-1);

        public bool StopOnException { get; set; } = false;
    }
}
