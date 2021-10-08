using System;
using System.Threading.Tasks;
using dotWork.Extensions;
using dotWork.Tests.Stubs;
using dotWork.Tests.Works;
using dotWork.Tests.WorkStubs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace dotWork.Tests
{
    public class Invoking_Tests
    {
        static readonly TimeSpan OneMillisecond = TimeSpan.FromMilliseconds(1);

        [Theory]
        [InlineData(typeof(Work_Sync_No_Parameters))]
        [InlineData(typeof(Work_Sync_With_CancellationToken))]
        [InlineData(typeof(Work_Sync_With_Dependency_And_CancellationToken))]

        [InlineData(typeof(Work_Async_No_Parameters))]
        [InlineData(typeof(Work_Async_With_CancellationToken))]
        [InlineData(typeof(Work_Async_With_Dependency_And_CancellationToken))]
        public async Task Iteration_Properly_Executes(Type workType)
        {
            // Arrange
            var host = new HostBuilder()
                .ConfigureServices(s =>
                {
                    s.AddTransient<StubDependency>();
                    s.AddWork(workType, typeof(DefaultWorkOptions));
                })
                .Build();

            // Act & Assert
            host.Start();
            await host.StopAsync();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Iterations_Do_Not_Stop_If_StopOnException_Is_False(bool stopOnException)
        {
            // Arrange
            var host = new HostBuilder()
                .ConfigureServices(s =>
                {
                    s.AddTransient<StubDependency>();
                    s.AddWork(typeof(Work_Async_With_Execution_Counter_Throws_Exception), typeof(DefaultWorkOptions), opt =>
                    {
                        opt.DelayBetweenIterations = TimeSpan.Zero;
                        opt.StopOnException = stopOnException;
                    });
                })
                .Build();
            var work = (Work_Async_With_Execution_Counter_Throws_Exception)host.Services.GetRequiredService(typeof(Work_Async_With_Execution_Counter_Throws_Exception));

            // Act 
            host.Start();
            await Task.Delay(TimeSpan.FromSeconds(0.1));
            await host.StopAsync();

            // Assert
            if (stopOnException)
                Assert.Equal(1, work.ExecutedIterationsCount);
            else
                Assert.True(work.ExecutedIterationsCount >= 2);
        }

        [Fact]
        public async Task Sync_Works_Do_Not_Deadlock_Startup_Process()
        {
            // Arrange
            var host = new HostBuilder()
                .ConfigureServices(s =>
                {
                    s.AddWork(typeof(Work_Sync_With_Counter1), typeof(DefaultWorkOptions));
                    s.AddWork(typeof(Work_Sync_With_Counter2), typeof(DefaultWorkOptions));
                })
                .Build();
            var work1 = (Work_Sync_With_Counter1)host.Services.GetRequiredService(typeof(Work_Sync_With_Counter1));
            var work2 = (Work_Sync_With_Counter2)host.Services.GetRequiredService(typeof(Work_Sync_With_Counter2));

            // Act 
            host.Start();
            await Task.Delay(TimeSpan.FromSeconds(0.1));
            await host.StopAsync();

            // Assert
            Assert.True(work1.ExecutedIterationsCount > 0);
            Assert.True(work2.ExecutedIterationsCount > 0);
        }

        [Fact]
        public async Task Invoke_Creates_Lifetime_Scope()
        {
            // Arrange
            var host = new HostBuilder()
                .ConfigureServices(s =>
                {
                    s.AddTransient<StubDependency>();
                    s.AddWork(typeof(Work_Ensure_Iteration_Lifetime), typeof(DefaultWorkOptions), opt =>
                    {
                        opt.DelayBetweenIterations = TimeSpan.Zero;
                    });
                })
                .Build();
            var work = (Work_Ensure_Iteration_Lifetime)host.Services.GetRequiredService(typeof(Work_Ensure_Iteration_Lifetime));

            // Act 
            host.Start();
            await Task.Delay(TimeSpan.FromSeconds(0.1));
            await host.StopAsync();

            // Assert
            Assert.True(work.ExecutedIterationsCount > 0);
            // Lifetime assertion is done inside work's ExecuteIteration method.
        }

        [Fact]
        public async Task Missing_Iteration_Lifetime_Dependency_Throws()
        {
            // Arrange
            var host = new HostBuilder()
                .ConfigureServices(s =>
                {
                    s.AddWork(typeof(Work_Sync_With_Dependency_And_CancellationToken), typeof(DefaultWorkOptions));
                })
                .Build();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => host.Start());
        }
    }
}
