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

        /// <inheritdoc />
        public virtual ValueTask<bool> OnIterationException(Exception ex) => ValueTask.FromResult(false);
    }
}
