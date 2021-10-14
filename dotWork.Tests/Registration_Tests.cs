using dotWork.Extensions;
using dotWork.Tests.OptionsStubs;
using dotWork.Tests.WorkStubs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace dotWork.Tests
{
    public class Registration_Tests
    {
        [Fact]
        public void AddWorks_Properly_Configures_Work()
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

        [Fact]
        public void Reregistration_Works()
        {
            // Removes old host
            // Changes work options type

            // Arrange
            var host = new HostBuilder()
                .ConfigureServices((ctx, s) =>
                {
                    var cfg = ctx.Configuration;
                    s.AddWorks(cfg.GetSection("Works"));
                    s.AddWork<Work_Implementing_IWork, DefaultWorkOptions2>();
                })
                .Build();
            var hostedServices = host.Services.GetServices<IHostedService>();
            var workBase = hostedServices.Single();

            // Act 

            // Assert
            Assert.Single(hostedServices); // Ensure we remove a previous WorkBase registration so that we don't end up with two hosted services.
            Assert.IsType<WorkBase<Work_Implementing_IWork, DefaultWorkOptions2>>(workBase); // Ensure the options type is actually changed.
        }
    }
}