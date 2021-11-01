using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace dotWork
{
    class WorkHost<TWork, TWorkOptions> : BackgroundService
        where TWork : class, IWork<TWorkOptions>
        where TWorkOptions : class, IWorkOptions
    {
        const string ExecuteIterationMethodName = "ExecuteIteration";

        readonly IServiceProvider _services;
        readonly ILogger _logger;
        readonly TWork _work;
        readonly IterationMethodMetadata _metadata;

        internal TWorkOptions WorkOptions;

        public WorkHost(IServiceProvider services, ILoggerFactory loggerFac, TWork work)
        {
            _services = services;
            _logger = loggerFac.CreateLogger(typeof(WorkHost<,>).FullName![..^2] + "." + typeof(TWork).Name);
            _work = work;
            _metadata = CreateMetadata();
            WorkOptions = GetWorkOptions();
            _work.Options = WorkOptions;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!WorkOptions.IsEnabled)
            {
                _logger.LogWarning("Work is disabled.");
                return;
            }

            _logger.LogInformation("Starting work.");

            while (!stoppingToken.IsCancellationRequested)
            {
                await ExecuteIterationSafe(stoppingToken);
                var delay = WorkOptions.DelayBetweenIterationsInSeconds == Timeout.Infinite
                    ? Timeout.InfiniteTimeSpan
                    : TimeSpan.FromSeconds(WorkOptions.DelayBetweenIterationsInSeconds);
                await Task.Delay(delay, stoppingToken);
            }

            _logger.LogInformation("Work is stopped.");
        }

        async Task ExecuteIterationSafe(CancellationToken stoppingToken)
        {
            // By resolving scoped services outside try/catch we ensure that missing dependencies 
            // will blow up the host regardless of _workOptions.StopOnException setting.
            var arguments = GetScopedArguments(stoppingToken, out var scope);
            try
            {
                _logger.LogTrace("Executing iteration...");
                await ExecuteIterationInternal(arguments);
                _logger.LogTrace("Iteration executed.");
            }
            catch (Exception ex)
            {
                OnIterationException?.Invoke(this, ex);
                _logger.LogError(ex, "Exception during iteration.");
                if (WorkOptions.StopOnException)
                {
                    _logger.LogError("No more iterations of this work will be executed due to an unhandled exception.");
                    throw;
                }
            }
            finally
            {
                scope.Dispose();
            }
        }

        async Task ExecuteIterationInternal(object?[] arguments)
        {
            await (_metadata.IsAsync
                    ? ExecuteAsynchronousIteration(arguments)
                    : ExecuteSyncronousIterationAsynchronously(arguments));
        }

        Task ExecuteAsynchronousIteration(object?[] arguments)
        {
            Debug.Assert(_metadata.IsAsync);
            var result = (Task)_metadata.Invoke(_work, arguments)!;
            return result;
        }

        Task ExecuteSyncronousIterationAsynchronously(object?[] arguments)
        {
            Debug.Assert(!_metadata.IsAsync);
            return Task.Run(async () =>
            {
                // Force the continuation to run async, making sure sync works 
                // do not block the whole host startup process.
                await Task.Yield();
                try
                {
                    _metadata.Invoke(_work, arguments);
                }
                catch (TargetInvocationException ex)
                {
                    // Due to use of reflection exceptions happened inside invoked iteration's method
                    // get wrapped in TargetInvocationException but only for non-async invoked methods.
                    // We unwrap the TargetInvocationException here to better match users' expectations.
                    throw ex.InnerException!;
                }
            });
        }

        IterationMethodMetadata CreateMetadata()
        {
            var methodInfo = _work.GetType().GetMethod(ExecuteIterationMethodName);
            if (methodInfo == null)
                throw new InvalidOperationException($"{typeof(TWork).Name} does not contain a public {ExecuteIterationMethodName} method.");

            var parameters = methodInfo.GetParameters();

            var returnType = methodInfo.ReturnType;
            bool isAsync = IsMethodAsync(methodInfo);
            if (returnType != typeof(void) && returnType != typeof(Task))
            {
                throw new InvalidOperationException($"{ExecuteIterationMethodName} method must return either void or Task.");
            }

            InvokeDelegate invokeRef = methodInfo.Invoke;
            var metadata = new IterationMethodMetadata(invokeRef, parameters, isAsync);
            return metadata;
        }

        object?[] GetScopedArguments(CancellationToken stoppingToken, out IDisposable scope)
        {
            // TODO do not create scope if we have no deps except CancellationToken.

            scope = _services.CreateScope();
            var provider = ((IServiceScope)scope).ServiceProvider;

            var parameters = _metadata.Parameters;
            var arguments = new object?[parameters.Length];

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                object? resolved;
                if (parameter.ParameterType == typeof(CancellationToken))
                {
                    resolved = stoppingToken;
                }
                else
                {
                    bool isOptional = parameter.IsOptional;
                    if (isOptional)
                        resolved = provider.GetService(parameter.ParameterType);
                    else
                        resolved = provider.GetRequiredService(parameter.ParameterType);
                }
                arguments[i] = resolved;
            }
            return arguments;
        }

        TWorkOptions GetWorkOptions()
        {
            var monitor = _services.GetRequiredService<IOptionsMonitor<TWorkOptions>>();
            var options = monitor.Get(typeof(TWork).Name);
            monitor.OnChange(OnWorkOptionsChanged);
            return options;
        }

        void OnWorkOptionsChanged(TWorkOptions options, string name)
        {
            if (name != typeof(TWork).Name)
                return;

            WorkOptions = options;
            _work.Options = WorkOptions;
            _work.OnOptionsChanged();

            _logger.LogInformation("Work options reloaded.");
        }

        static bool IsMethodAsync(MethodInfo method)
        {
            Type attType = typeof(AsyncStateMachineAttribute);

            // Obtain the custom attribute for the method. 
            // The value returned contains the StateMachineType property. 
            // Null is returned if the attribute isn't present for the method. 
            var attrib = (AsyncStateMachineAttribute?)method.GetCustomAttribute(attType);

            return attrib != null;
        }

        // TODO since we only need that for testing, we should try to access the BackgroundService._executingTask
        // using reflection instead.
        internal event EventHandler<Exception>? OnIterationException; 
    }
}
