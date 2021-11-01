using System.Threading;
using System.Threading.Tasks;
using dotWork.Tests.WorkStubs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
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
    }
}
