using System;

namespace dotWork
{
    public interface IWorkOptions
    {
        public TimeSpan DelayBetweenIterations { get; set; }

        /// <summary>
        /// If `true`, no more iterations are executed after an unhandled exception occurs.
        /// </summary>
        public bool StopOnException { get; set; }
    }
}
