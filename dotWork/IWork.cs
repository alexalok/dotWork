using System;
using System.Threading.Tasks;

namespace dotWork
{
    public interface IWork<TWorkOptions> where TWorkOptions : class, IWorkOptions
    {
        TWorkOptions Options { get; set; }

        void OnOptionsChanged();

        /// <summary>
        ///     Called on an unhandled exception inside the iteration.
        /// </summary>
        /// <param name="ex">Instance of an unhandled exception.</param>
        /// <returns>
        ///     `true` if work must be forcefully stopped,
        ///     `false` if <see cref="IWorkOptions.StopOnException" /> decides (default).
        /// </returns>
        ValueTask<bool> OnIterationException(Exception ex) => ValueTask.FromResult(false);
    }
}