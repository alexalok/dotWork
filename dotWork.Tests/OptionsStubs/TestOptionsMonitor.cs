using System;
using Microsoft.Extensions.Options;

namespace dotWork.Tests.OptionsStubs
{
    public class TestOptionsMonitor<TWorkOptions> : IOptionsMonitor<TWorkOptions> where TWorkOptions : IWorkOptions
    {
        public TWorkOptions CurrentValue { get; }

        public TestOptionsMonitor(TWorkOptions options)
        {
            CurrentValue = options;
        }

        public TWorkOptions Get(string name) => CurrentValue;

        public IDisposable OnChange(Action<TWorkOptions, string> listener) => new StubDisposable();

        public class StubDisposable : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}