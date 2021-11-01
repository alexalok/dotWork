using System.Threading;

namespace dotWork
{
    /// <summary>
    ///     <see cref="IsEnabled" />: <i>true</i>.<br />
    ///     <see cref="DelayBetweenIterationsInSeconds" />: <i>Infinite</i>.<br />
    ///     <see cref="StopOnException" />: <i>false</i>.
    /// </summary>
    public class DefaultWorkOptions : IWorkOptions
    {
        public bool IsEnabled { get; set; } = true;

        public int DelayBetweenIterationsInSeconds { get; set; } = Timeout.Infinite;

        public bool StopOnException { get; set; } = false;
    }
}
