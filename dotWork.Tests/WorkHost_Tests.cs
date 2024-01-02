using dotWork.Tests.OptionsStubs;
using dotWork.Tests.TestExceptions;
using dotWork.Tests.WorkStubs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace dotWork.Tests
{
    public class WorkHost_Tests
    {
        /// <summary>
        ///     Ensure that the default options are safe.
        ///     Issue: https://github.com/alexalok/dotWork/issues/3
        /// </summary>
        [Fact]
        public async Task ExecuteAsync_Does_Not_Fail_With_DefaultWorkOptions()
        {
            // Arrange
            ServiceProvider? provider = new ServiceCollection()
                .AddOptions<DefaultWorkOptions>(nameof(Work_Async_No_Parameters))
                .Services.BuildServiceProvider();
            var work = new Work_Async_No_Parameters();
            var workHost = new WorkHost<Work_Async_No_Parameters, DefaultWorkOptions>(provider, new NullLoggerFactory(), work);

            // Act
            await workHost.StartAsync(CancellationToken.None);
            var executingTask = workHost.GetExecutingTask();

            // Assert
            Assert.False(executingTask.IsFaulted);
        }

        [Fact]
        public async Task WorkHost_Accepts_Work_With_Async_Iteration_Suffix()
        {
            // Arrange, Act, Assert
            ServiceProvider? provider = new ServiceCollection()
                .AddOptions<DefaultWorkOptions>(nameof(Work_With_Async_Iteration_Suffix))
                .Services.BuildServiceProvider();
            var work = new Work_With_Async_Iteration_Suffix();
            var workHost =
                new WorkHost<Work_With_Async_Iteration_Suffix, DefaultWorkOptions>(provider, new NullLoggerFactory(),
                    work); // calls CreateMetadata() which throws if it can't find the iteration method
        }

        [Theory]
        [InlineData(false, false, false)] // We stop if either work options or exception handler indicate
        [InlineData(true, false, true)] // that we need to stop 
        [InlineData(false, true, true)]
        [InlineData(true, true, true)]
        public async Task Ensure_WorkBase_OnIterationException_Return_Value_Is_Respected(
            bool shouldStopOnException, bool shouldForceStopOnException, bool isStopExpected)
        {
            // Arrange
            DefaultWorkOptions workOptions = new() { StopOnException = shouldStopOnException, DelayBetweenIterationsInSeconds = 0 };

            Mock<IServiceProvider> services = new(MockBehavior.Strict);
            services.Setup(s => s.GetService(typeof(IOptionsMonitor<DefaultWorkOptions>)))
                .Returns(new TestOptionsMonitor<DefaultWorkOptions>(workOptions));

            Mock<IServiceScope> scope = new(MockBehavior.Strict);
            scope.Setup(s => s.ServiceProvider).Returns(services.Object);
            scope.Setup(s => s.Dispose());

            Mock<IServiceScopeFactory> scopeFactory = new(MockBehavior.Strict);
            scopeFactory.Setup(sf => sf.CreateScope()).Returns(scope.Object);

            services.Setup(s => s.GetService(typeof(IServiceScopeFactory))).Returns(scopeFactory.Object);

            Mock<IAsyncWork> workBase = new(MockBehavior.Strict);
            workBase.SetupProperty(wb => wb.Options);
            workBase.Setup(wb => wb.OnIterationException(It.IsAny<IterationFinishedException>()))
                .Returns(ValueTask.FromResult(shouldForceStopOnException));
            workBase.Setup(wb => wb.ExecuteIteration())
                .Throws<IterationFinishedException>();

            var workHost = new WorkHost<IAsyncWork, DefaultWorkOptions>(services.Object, new NullLoggerFactory(), workBase.Object);

            AutoResetEvent workStoppedEvent = new(false);
            AutoResetEvent iterExEvent = new(false);
            workHost.OnWorkStopped += (_, ex) => workStoppedEvent.Set();
            workHost.OnIterationException += (_, ex) => { iterExEvent.Set(); };

            // Act
            await workHost.StartAsync(CancellationToken.None);
            iterExEvent.WaitOne(); // One iteration always passes.

            // Assert
            if (isStopExpected)
            {
                workBase.Verify(wb => wb.ExecuteIteration(), Times.Once);
                workStoppedEvent.WaitOne(); // Ensure work is stopped.
            }
            else
            {
                iterExEvent.WaitOne(); // At least one more iteration must pass.
                workBase.Verify(wb => wb.ExecuteIteration(), Times.AtLeast(2));
            }
        }

        [Fact]
        public async Task Skips_Delay_If_Requested_During_Iteration()
        {
            // Arrange
            DefaultWorkOptions opt = new();

            Mock<IServiceProvider> servicesStub = new();
            servicesStub.Setup(s => s.GetService(typeof(IOptionsMonitor<DefaultWorkOptions>)))
                .Returns(new TestOptionsMonitor<DefaultWorkOptions>(opt));

            Mock<IServiceScope> scopeStub = new();
            scopeStub.Setup(s => s.ServiceProvider).Returns(servicesStub.Object);

            Mock<IServiceScopeFactory> scopeFactoryStub = new();
            scopeFactoryStub.Setup(sf => sf.CreateScope()).Returns(scopeStub.Object);

            servicesStub.Setup(s => s.GetService(typeof(IServiceScopeFactory))).Returns(scopeFactoryStub.Object);

            Work_Skips_Delay_During_Iteration workMock = new();
            var workHost = new WorkHost<Work_Skips_Delay_During_Iteration, DefaultWorkOptions>(servicesStub.Object, new NullLoggerFactory(), workMock);
            int iterationsCount = 0;
            TaskCompletionSource tcs = new();
            CancellationTokenSource cts = new(TimeSpan.FromSeconds(1));
            cts.Token.Register(tcs.SetCanceled);
            workHost.OnIterationFinished += async (_, e) =>
            {
                iterationsCount++;
                if (iterationsCount > 1)
                {
                    tcs.TrySetResult();
                }
            };

            // Act
            await workHost.StartAsync(cts.Token);
            await tcs.Task;

            // Assert
            // No exception, meaning we had at least two iterations on a work with an infinite delay
            // between them which means we skipped the delay.
        }

        [Fact]
        public async Task Skips_Delay_If_Requested_During_Delay_Between_Iterations()
        {
            // Arrange
            DefaultWorkOptions2 opt = new();

            Mock<IServiceProvider> servicesStub = new();
            servicesStub.Setup(s => s.GetService(typeof(IOptionsMonitor<DefaultWorkOptions2>)))
                .Returns(new TestOptionsMonitor<DefaultWorkOptions2>(opt));

            Mock<IServiceScope> scopeStub = new();
            scopeStub.Setup(s => s.ServiceProvider).Returns(servicesStub.Object);

            Mock<IServiceScopeFactory> scopeFactoryStub = new();
            scopeFactoryStub.Setup(sf => sf.CreateScope()).Returns(scopeStub.Object);

            servicesStub.Setup(s => s.GetService(typeof(IServiceScopeFactory))).Returns(scopeFactoryStub.Object);

            Work_With_DefaultWorkOptions2 workMock = new();
            var workHost = new WorkHost<Work_With_DefaultWorkOptions2, DefaultWorkOptions2>(servicesStub.Object, new NullLoggerFactory(), workMock);

            int iterationsCount = 0;
            TaskCompletionSource firstIterationFinishedTcs = new();
            TaskCompletionSource secondIterationFinishedTcs = new();
            CancellationTokenSource cts = new(TimeSpan.FromSeconds(1));
            cts.Token.Register(secondIterationFinishedTcs.SetCanceled);
            workHost.OnIterationFinished += async (_, e) =>
            {
                iterationsCount++;
                if (iterationsCount == 1)
                {
                    firstIterationFinishedTcs.SetResult();
                }
                if (iterationsCount > 1)
                {
                    secondIterationFinishedTcs.TrySetResult();
                }
            };

            // Act
            await workHost.StartAsync(cts.Token);
            await firstIterationFinishedTcs.Task;
            workMock.SkipDelayOnce();
            await secondIterationFinishedTcs.Task;

            // Assert
            // No exception, meaning we had at least two iterations on a work with an infinite delay
            // between them which means we skipped the delay.
        }
    }
}