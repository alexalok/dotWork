# About

## What is dotWork?
dotWork is a library aimed to ease the development of [Worker Services](https://docs.microsoft.com/en-us/dotnet/core/extensions/workers) in .NET.
More specifically, it extends an existing [BackgroundService](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting.backgroundservice) class in order to reduce the number of a boilerplate code required to set up a Worker Service.

## When should I use dotWork?

You should consider using dotWork if you find yourself repeating the same boilerplate code required to set up simple and complex works alike.

# Installation

## Create a new worker project (optional)

### Using Visual Studio

To create a new Worker Service project with Visual Studio, you'd select **File** > **New** > **Project...**. From the **Create a new project** dialog search for "Worker Service", and select Worker Service template. If you'd rather use the .NET CLI, open your favorite terminal in a working directory. Run the `dotnet new` command, and replace the `<Project.Name>` with your desired project name.

### Using .NET CLI

```c#
dotnet new worker --name <Project.Name>
```

## Install a NuGet package

### Using Visual Studio

Install the `dotWork` package by following [the instructions](https://docs.microsoft.com/en-us/nuget/quickstart/install-and-use-a-package-in-visual-studio). Make sure to check the **Include prerelease** checkbox because there is no release version of dotWork yet.

### Using .NET CLI

```c#
dotnet add package dotWork --prerelease
```

At this point the dotWork package is installed in your project but is not used. dotWork will not have any side-effects on your application unless explicitly used. Note that the `--prerelease` argument is mandatory because there is no release version of dotWork yet.

**Please note** that the worker project template automatically creates an example work class. You should delete it and create a new one by following the instructions below.

# Usage

## Creating a work file

The first step of using dotWork is creating a work class itself. 

```c#
using dotWork;
...
public class ExampleWork : WorkBase<DefaultWorkOptions>
{
    
}
```

There are two things to notice:

1. You need to add a `using dotWork` directive to be able access the `WorkBase` type. 
2. You need to inherit your work class from `WorkBase<TWorkOptions>`, where `TWorkOptions` can be any class that implements `IWorkOptions` interface. For simplicity, in this example we will use `DefaultWorkOptions` class that is provided alongside the library. 

## Defining an iteration code

Now we need to write a method that describes what is needed to be done in each work iteration. dotWork will use this method to periodically execute a work iteration. We will learn how we can configure a delay between iterations later.

For dotWork to be able to find the iteration method it needs to satisfy a number of conditions:

1. Have a name `ExecuteIteration`.
2. Be `public`.
3. Return either `void` or `Task`.

Let's create one:

```c#
public class ExampleWork : WorkBase<DefaultWorkOptions>
{
    public async Task ExecuteIteration(CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromSeconds(1), ct);
        Console.WriteLine("Work iteration finished!");
    }
}
```

This is a simple iteration that waits for a second and then prints to console. Notice that we have specified a `CancellationToken ct` argument. It will be automatically provided by dotWork and will be triggered once the application host starts performing a shutdown. 

Note that the `CancellationToken` is not the only argument you can specify in the iteration method definition. In fact, you can specify any reference type as an argument and dotWork will try to resolve it from an application's service provider in a scoped manner. More on that later. 

## Registering the work with the container

Once we're finished creating a work class we also need to register it with our host container. We can do it by adding the following line to our HostBuilder's `ConfigureServices` method:

```c#
services.AddWork<ExampleWork, DefaultWorkOptions>();
```

* `ExampleWork` is our work type.
* `DefaultWorkOptions` is the work's options type.

After that the `ConfigureServices` method should look like that:

```c#
.ConfigureServices(services =>
{
    services.AddWork<ExampleWork, DefaultWorkOptions>();
})
```

At this point our Worker Service is ready and we can already test it by running the project. You can notice, however, that the work iteration is only executed once. This is because we haven't configured the work options. By default, work iteration is only executed once. We can change that by configuring the options like so:

```c#
services.AddWork<ExampleWork, DefaultWorkOptions>(configure: opt =>
{
    opt.DelayBetweenIterationsInSeconds = 10;
});
```

Now the second iteration will execute 10 seconds **after the first iteration finishes**.

**TIP:** You can use your own options classes by inheriting `IWorkOptions` interface or even `DefaultWorkOptions` type itself.

## Service injection

dotWork supports two ways to inject services into works. You are free to use any combination of these.

### Singleton injection

You can inject any singleton service into the work by adding a corresponding argument to the work's constructor, just like you would normally do:

```c#
public class ExampleWork : WorkBase<DefaultWorkOptions>
{
    readonly SingletonService _singletonService;

    public ExampleWork(SingletonService singletonService)
    {
        _singletonService = singletonService;
    }

	...
}
```

### Scoped injection

If you have any scoped services you would like to use with your works then you may inject them directly into the `ExecuteIteration` method. A new scope and thus a new instance of a service will be created for each iteration.

```c#
public async Task ExecuteIteration(ScopedService scopedService, CancellationToken ct)
{
    await Task.Delay(TimeSpan.FromSeconds(1), ct);
    Console.WriteLine("Work iteration finished!");
}
```

# Advanced scenarios

## Batch registration

It is possible to register all works at once by calling an `AddWorks` extension method. **It is mandatory** to provide works configuration section. This configuration section must contain a dictionary of configuration sections for each work with the **key value being a name of the work**. For example, if we have two works:

* `ExampleWork1`
* `ExampleWork2`

then we should have an `appSettings.json` file that looks something like that:

```json
{
    "Works": {
        "ExampleWork1": {
            "IsEnabled": false,
            "DelayBetweenIterationsInSeconds": 86400 // 1 day
        },
        "ExampleWork2": {
            "DelayBetweenIterationsInSeconds": 3600 // 1 hour
        }
    }
}
```

And the registration code should look somewhat like that:

```c#
.ConfigureServices((ctx, services) =>
{
    var cfg = ctx.Configuration;
    services.AddWorks(cfg.GetSection("Works"));
})
```

**TIP:** it is possible to override the specific work's registration by calling `AddWork` on it **after** an `AddWorks` call:

```c#
.ConfigureServices((ctx, services) =>
{
    var cfg = ctx.Configuration;
    services.AddWorks(cfg.GetSection("Works"));
    services.AddWork<ExampleWork1, DefaultWorkOptions>(configure: opt =>
    {
        opt.DelayBetweenIterationsInSeconds = 86400 * 2; // 2 days
    });
})
```



