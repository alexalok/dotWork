using System;
using System.Threading.Tasks;

namespace dotWork
{
    public abstract class WorkBase<TWorkOptions> : IWork<TWorkOptions>
        where TWorkOptions : class, IWorkOptions
    {
        public TWorkOptions Options { get; set; } = null!;

        public virtual void OnOptionsChanged()
        {
        }

        /// <summary>
        /// Skip delay before the next iteration starts. If current iteration is being executed, 
        /// the next iteration will start as soon as the current one finishes. 
        /// If work is currently waiting for the next iteration to start, it will start immediately.
        /// Subsequent calls of this method will have no effect until the next iteration starts.
        /// </summary>
        protected internal void SkipDelayOnce()
        {
            SkipDelayRequested?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler? SkipDelayRequested;

        /// <inheritdoc />
        public virtual ValueTask<bool> OnIterationException(Exception ex) => ValueTask.FromResult(false);
    }
}
