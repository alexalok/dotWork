namespace dotWork
{
    public interface IWorkOptions
    {
        public bool IsEnabled { get; set; }

        public int DelayBetweenIterationsInSeconds { get; set; }

        /// <summary>
        /// If `true`, no more iterations are executed after an unhandled exception occurs.
        /// </summary>
        public bool StopOnException { get; set; }
    }
}
