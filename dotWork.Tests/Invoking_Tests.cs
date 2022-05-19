using System;
using System.Linq;
using System.Threading.Tasks;
using dotWork.Extensions;
using dotWork.Tests.Stubs;
using dotWork.Tests.TestExceptions;
using dotWork.Tests.WorkStubs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace dotWork.Tests
{
    public class Invoking_Tests
    {
        [Theory]
        [InlineData(typeof(Work_Sync_No_Parameters))]
        [InlineData(typeof(Work_Sync_With_CancellationToken))]
        [InlineData(typeof(Work_Sync_With_Dependency_And_CancellationToken))]
        [InlineData(typeof(Work_Async_No_Parameters))]
        [InlineData(typeof(Work_Async_With_CancellationToken))]
        [InlineData(typeof(Work_Async_With_Dependency_And_CancellationToken))]
        [InlineData(typeof(Work_With_ValueTask))]
        public async Task Iteration_Properly_Executes(Type workType)
        {
            // Arrange
            var host = new HostBuilder()
                .ConfigureServices(s =>
                {
                    s.AddTransient<StubDependency>();
                    s.AddWork(workType);
                })
                .Build();

            // Act & Assert
            host.Start();
            await host.StopAsync();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task StopOnException_Is_Respected(bool stopOnException)
        {
            // Arrange
            var host = new HostBuilder()
                .ConfigureServices(s =>
                {
                    s.AddTransient<StubDependency>();
                    s.AddWork(typeof(Work_Async_With_Execution_Counter_Throws_Exception), configure: opt =>
                    {
                        opt.DelayBetweenIterationsInSeconds = 0;
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
                    s.AddWork(typeof(Work_Sync_With_Counter1));
                    s.AddWork(typeof(Work_Sync_With_Counter2));
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
                    s.AddWork(typeof(Work_Ensure_Iteration_Lifetime), configure: opt =>
                    {
                        opt.DelayBetweenIterationsInSeconds = 0;
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
                    s.AddWork(typeof(Work_Sync_With_Dependency_And_CancellationToken));
                })
                .Build();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => host.Start());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task IsEnabled_Respected(bool isEnabled)
        {
            // Arrange
            var host = new HostBuilder()
                .ConfigureServices(s =>
                {
                    s.AddWork(typeof(Work_Async_With_Execution_Counter_Throws_Exception), configure: opt =>
                    {
                        opt.IsEnabled = isEnabled;
                    });
                })
                .Build();
            var work = (Work_Async_With_Execution_Counter_Throws_Exception)host.Services.GetRequiredService(typeof(Work_Async_With_Execution_Counter_Throws_Exception));

            // Act 
            host.Start();
            await Task.Delay(TimeSpan.FromSeconds(0.1));
            await host.StopAsync();

            // Assert
            int expectedIterationsCount = isEnabled ? 1 : 0;
            Assert.Equal(expectedIterationsCount, work.ExecutedIterationsCount);
        }

        [Fact]
        public async Task Sync_Invocation_Exception_Is_Unwrapped()
        {
            // Arrange
            var host = new HostBuilder()
                .ConfigureServices(s =>
                {
                    s.AddWork(typeof(Work_Sync_Throws));
                })
                .Build();

            var workBase = (WorkHost<Work_Sync_Throws, DefaultWorkOptions>)
                host.Services.GetServices<IHostedService>()
                .Single(s => s.GetType() == typeof(WorkHost<Work_Sync_Throws, DefaultWorkOptions>));

            Exception? thrownEx = null;
            workBase.OnIterationException += (_, ex) =>
            {
                thrownEx = ex;
            };

            // Act 
            host.Start();
            await Task.Delay(TimeSpan.FromSeconds(0.1));
            await host.StopAsync();

            // Assert
            Assert.IsType<IterationFinishedException>(thrownEx);
        }

        [Fact]
        public async Task Ensure_OnIterationException_Is_Invoked_On_Iteration_Exception()
        {
            // Arrange
            var host = new HostBuilder()
                .ConfigureServices(s => { s.AddWork(typeof(Work_Async_With_Execution_Counter_Throws_Exception)); })
                .Build();

            var workBase = (WorkHost<Work_Async_With_Execution_Counter_Throws_Exception, DefaultWorkOptions>)
                host.Services.GetServices<IHostedService>()
                    .Single(s => s.GetType() == typeof(WorkHost<Work_Async_With_Execution_Counter_Throws_Exception, DefaultWorkOptions>));

            bool isInvoked = false;
            workBase.OnIterationException += (_, _) => { isInvoked = true; };

            // Act 
            host.Start();
            await Task.Yield();
            await host.StopAsync();

            // Assert
            Assert.True(isInvoked);
        }
    }
}
