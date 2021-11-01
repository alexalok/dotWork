using dotWork;
using dotWork.Example;
using dotWork.Extensions;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        var cfg = ctx.Configuration;
        services.AddWorks(cfg.GetSection("Works"));
        services.AddWork<ExampleWork, DefaultWorkOptions>(configure: opt =>
        {
            opt.DelayBetweenIterationsInSeconds = 3600; // 1 hour
        });
    })
    .Build();

await host.RunAsync();
