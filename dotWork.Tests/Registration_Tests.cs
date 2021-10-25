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
                        {"Works:Work_Async_No_Parameters:IsEnabled", "false" },
                        {"Works:Work_Async_No_Parameters:DelayBetweenIterationsInSeconds", "1000" }
                    };
                    cfg.AddInMemoryCollection(worksOptions);
                })
                .ConfigureServices((ctx, s) =>
                {
                    var cfg = ctx.Configuration;
                    s.AddWorks(cfg.GetSection("Works"));
                })
                .Build();

            var workBase = (WorkHost<Work_Async_No_Parameters, DefaultWorkOptions>)
                host.Services.GetServices<IHostedService>()
                    .Single(s => s.GetType() == typeof(WorkHost<Work_Async_No_Parameters, DefaultWorkOptions>));

            // Act 

            // Assert
            Assert.False(workBase.WorkOptions.IsEnabled);
            Assert.Equal(TimeSpan.FromSeconds(1000), workBase.WorkOptions.DelayBetweenIterations);
        }

        /// <summary>
        /// Ensure we can register a work and then override its registration.
        /// </summary>
        [Fact]
        public void Reregistration_Override_Works()
        {
            // Arrange
            var host = new HostBuilder()
                .ConfigureAppConfiguration(cfg =>
                {
                    var worksOptions = new Dictionary<string, string>()
                    {
                        {"Works:Work_With_DefaultWorkOptions2:DelayBetweenIterationsInSeconds", "1000" }
                    };
                    cfg.AddInMemoryCollection(worksOptions);
                })
                .ConfigureServices((ctx, s) =>
                {
                    var cfg = ctx.Configuration;
                    s.AddWorks(cfg.GetSection("Works"), t => t == typeof(Work_With_DefaultWorkOptions2));
                    s.AddWork<Work_With_DefaultWorkOptions2, DefaultWorkOptions2>(configure: opt =>
                    {
                        opt.IsEnabled = false;
                    });
                })
                .Build();
            var hostedServices = host.Services.GetServices<IHostedService>();
            var workHost = (WorkHost<Work_With_DefaultWorkOptions2, DefaultWorkOptions2>)hostedServices.Single();

            // Act 

            // Assert
            Assert.False(workHost.WorkOptions.IsEnabled); // Ensure our override applied.
            Assert.Equal(1000, workHost.WorkOptions.DelayBetweenIterationsInSeconds); // Ensure our override preserved unchanged values from configuration.
        }
    }
}