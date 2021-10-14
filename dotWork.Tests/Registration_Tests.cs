using dotWork.Extensions;
using dotWork.Tests.WorkStubs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace dotWork.Tests
{
    public class Registration_Tests
    {
        [Fact]
        public async Task AddWorks_Properly_Configures_Work()
        {
            // Arrange
            var host = new HostBuilder()
                .ConfigureAppConfiguration(cfg =>
                {
                    var worksOptions = new Dictionary<string, string>()
                    {
                        {"Works:Work_Implementing_IWork:IsEnabled", "false" },
                        {"Works:Work_Implementing_IWork:DelayBetweenIterationsInSeconds", "1000" }
                    };
                    cfg.AddInMemoryCollection(worksOptions);
                })
                .ConfigureServices((ctx, s) =>
                {
                    var cfg = ctx.Configuration;
                    s.AddWorks(cfg.GetSection("Works"));
                })
                .Build();

            var workBase = (WorkBase<Work_Implementing_IWork, DefaultWorkOptions>)
                host.Services.GetServices<IHostedService>()
                    .Single(s => s.GetType() == typeof(WorkBase<Work_Implementing_IWork, DefaultWorkOptions>));

            // Act 

            // Assert
            Assert.False(workBase.WorkOptions.IsEnabled);
            Assert.Equal(TimeSpan.FromSeconds(1000), workBase.WorkOptions.DelayBetweenIterations);
        }
    }
}