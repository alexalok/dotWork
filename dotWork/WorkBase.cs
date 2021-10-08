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
    class WorkBase<TWork, TWorkOptions> : BackgroundService
        where TWork : class
        where TWorkOptions : class, IWorkOptions
    {
        const string ExecuteIterationMethodName = "ExecuteIteration";

        readonly IServiceProvider _services;
        readonly ILogger<WorkBase<TWork, TWorkOptions>> _logger;
        readonly TWork _work;
        readonly IterationMethodMetadata _metadata;
        IWorkOptions _workOptions;

        public WorkBase(IServiceProvider services, ILogger<WorkBase<TWork, TWorkOptions>> logger, TWork work)
        {
            _services = services;
            _logger = logger;
            _work = work;
            _workOptions = GetWorkOptions();
            _metadata = CreateMetadata();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting work.");

            // By resolving scoped services before any awaits we ensure that missing dependencies 
            // will blow up the host instead of being silently swallowed by BackgroundService's _executingTask.
            // TODO: we might use this scope once instead of just throwing it out.
            _ = GetScopedArguments(CancellationToken.None, out var scope);
            scope.Dispose();

            while (!stoppingToken.IsCancellationRequested)
            {
                await ExecuteIterationSafe(stoppingToken);
                await Task.Delay(_workOptions.DelayBetweenIterations, stoppingToken);
            }

            _logger.LogInformation("Work is stopped.");
        }

        async Task ExecuteIterationSafe(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogTrace("Executing iteration...");
                await ExecuteIterationInternal(stoppingToken);
                _logger.LogTrace("Iteration executed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during iteration.");
                if (_workOptions.StopOnException)
                {
                    _logger.LogError("No more iterations of this work will be executed due to an unhandled exception.");
                    throw;
                }
            }
        }

        async Task ExecuteIterationInternal(CancellationToken stoppingToken)
        {
            var arguments = GetScopedArguments(stoppingToken, out var scope);
            try
            {
                await (_metadata.IsAsync
                    ? ExecuteAsynchronousIteration(arguments)
                    : ExecuteSyncronousIterationAsynchronously(arguments));
            }
            finally
            {
                scope.Dispose();
            }
        }

        Task ExecuteAsynchronousIteration(object?[] arguments)
        {
            Debug.Assert(_metadata.IsAsync);
            var result = (Task)_metadata.Invoke(_work, arguments)!;
            return result;
        }

        async Task ExecuteSyncronousIterationAsynchronously(object?[] arguments)
        {
            Debug.Assert(!_metadata.IsAsync);

            try
            {
                await Task.Run(() =>
                {
                    _metadata.Invoke(_work, arguments);
                });
            }
            catch (TargetInvocationException ex)
            {
                // Due to use of reflection exceptions happened inside invoked iteration's method
                // get wrapped in TargetInvocationException but only for non-async methods.
                // We unwrap the TargetInvocationException here to better match users' expectations.
                throw ex.InnerException!;
            }
        }

        IterationMethodMetadata CreateMetadata()
        {
            var methodInfo = _work.GetType().GetMethod(ExecuteIterationMethodName);
            if (methodInfo == null)
                throw new InvalidOperationException($"{nameof(TWork)} does not contain a public {ExecuteIterationMethodName} method.");

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

        IWorkOptions GetWorkOptions()
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
            _workOptions = options;
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
    }
}
