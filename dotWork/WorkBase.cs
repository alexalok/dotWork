using System;
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
        readonly IWorkOptions _workOptions;

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
            if (!_metadata.IsAsync)
                await Task.Yield(); // Synchronous services will deadlock the startup process unless we yield manually.

            _logger.LogInformation("Starting work.");

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

        Task ExecuteIterationInternal(CancellationToken stoppingToken)
        {
            using var scope = _services.CreateScope();
            var provider = scope.ServiceProvider;

            var arguments = GetArguments(provider, stoppingToken);
            object? result;
            try
            {
                result = _metadata.Invoke(_work, arguments);
            }
            catch (TargetInvocationException ex)
            {
                if (!_metadata.IsAsync)
                {
                    // Due to use of reflection exceptions happened inside invoked iteration's method
                    // get wrapped in TargetInvocationException but only for non-async methods.
                    // We unwrap the TargetInvocationException here to better match users' expectations.
                    throw ex.InnerException!;
                }

                throw;
            }

            // We cannot rely on _metadata.IsAsync here because there may be non-async Task-returning (proxy) methods.
            return result is Task task ? task : Task.CompletedTask;
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

        object?[] GetArguments(IServiceProvider provider, CancellationToken stoppingToken)
        {
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
            var snapshot = _services.GetRequiredService<IOptionsSnapshot<TWorkOptions>>();
            return snapshot.Get(typeof(TWork).Name);
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
