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
        /// <summary>
        ///     Fired on an unhandled exception inside the iteration.
        ///     Can fire multiple times if work is set to continue after an unhandled exception
        ///     (see <see cref="IWorkOptions.StopOnException" />)
        /// </summary>
        public event EventHandler<Exception>? OnIterationException;

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

        async ValueTask ExecuteIterationSafe(CancellationToken stoppingToken)
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

        async ValueTask ExecuteIterationInternal(object?[] arguments)
        {
            if (_metadata.IsAsync)
                await ExecuteAsynchronousIteration(arguments);
            else
                await ExecuteSyncronousIterationAsynchronously(arguments);
        }

        async ValueTask ExecuteAsynchronousIteration(object?[] arguments)
        {
            Debug.Assert(_metadata.IsAsync);
            if (_metadata.IsValueTask)
                await (ValueTask) _metadata.Invoke(_work, arguments)!;
            else
                await (Task) _metadata.Invoke(_work, arguments)!;
        }

        async ValueTask ExecuteSyncronousIterationAsynchronously(object?[] arguments)
        {
            Debug.Assert(!_metadata.IsAsync);
            await Task.Run(async () =>
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
            MethodInfo? methodInfo = null;
            foreach (string executeIterationMethodName in Constants.ExecuteIterationMethodNames)
            {
                methodInfo = _work.GetType().GetMethod(executeIterationMethodName);
                if (methodInfo != null)
                    break;
            }

            if (methodInfo == null)
            {
                var names = string.Join(", ", Constants.ExecuteIterationMethodNames);
                throw new InvalidOperationException(
                    $"{typeof(TWork).Name} must contain a public method with one of the following names: [{names}].");
            }

            var parameters = methodInfo.GetParameters();

            var returnType = methodInfo.ReturnType;
            bool isAsync = IsMethodAsync(methodInfo);
            bool isValueTask = isAsync && returnType == typeof(ValueTask);

            InvokeDelegate invokeRef = methodInfo.Invoke;
            var metadata = new IterationMethodMetadata(invokeRef, parameters, isAsync, isValueTask);
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
            var attrib = (AsyncStateMachineAttribute?)CustomAttributeExtensions.GetCustomAttribute(method, attType);

            return attrib != null;
        }
    }
}
