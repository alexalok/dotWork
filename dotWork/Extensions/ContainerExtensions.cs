using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace dotWork.Extensions
{
    public static class ContainerExtensions
    {
        /// <summary>
        ///     Registers all or selected classes implementing <see cref="IWork{TWorkOptions}" /> interface as works with the
        ///     corresponding options.
        ///     and configures them from <see cref="IConfigurationSection" />.
        ///     <remarks>Only works contained in an assembly that called this method are registered.</remarks>
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration">Configuration root or section containing a dictionary of per-work configuration sections.</param>
        /// <param name="typeSelector">
        ///     If provided, each type implemeting <see cref="IWork{TWorkOptions}" /> will additionally be
        ///     tested against the selector and only registered if selector returns <b>true</b>.
        /// </param>
        public static IServiceCollection AddWorks(this IServiceCollection services, IConfiguration configuration, Func<Type, bool>? typeSelector = null)
        {
            if (configuration is null)
                throw new ArgumentNullException(nameof(configuration));

            var workTypes = Assembly.GetCallingAssembly().GetTypes().Where(t => t.ImplementsInterface(typeof(IWork<>)));
            foreach (var workType in workTypes)
            {
                if (typeSelector?.Invoke(workType) == false)
                    continue;
                var configSubSection = configuration.GetSection(workType.Name);
                AddWork(services, workType, configSubSection);
            }

            return services;
        }

        /// <summary>
        ///     Register single work with the container.
        /// </summary>
        /// <typeparam name="TWork">Type of work to be registered.</typeparam>
        /// <typeparam name="TWorkOptions">Type of work options to be registered.</typeparam>
        /// <param name="services"></param>
        /// <param name="configuration">
        ///     Configuration root or section that
        ///     <typeparamref name="TWorkOptions" /> will bind against.
        /// </param>
        /// <param name="configure">Delegate used to configure </param>
        /// <exception cref="ArgumentException">
        ///     <typeparamref name="TWorkOptions" /> specified does not match the one work type
        ///     inherits from.
        /// </exception>
        public static IServiceCollection AddWork<TWork, TWorkOptions>(this IServiceCollection services, IConfiguration? configuration = null, Action<TWorkOptions>? configure = null)
            where TWork : class, IWork<TWorkOptions>
            where TWorkOptions : class, IWorkOptions
        {
            var workOptionsType = TypeExtensions.GetWorkOptionsType(typeof(TWork));
            if (workOptionsType != typeof(TWorkOptions))
                throw new ArgumentException($"Work uses {workOptionsType.Name} but is registered with {typeof(TWorkOptions).Name}.");

            var optionsBuilder = services.AddOptions<TWorkOptions>(typeof(TWork).Name);
            if (configuration != null)
                optionsBuilder.Bind(configuration);
            if (configure != null)
                optionsBuilder.Configure(configure);

            optionsBuilder.Validate(opt => opt.DelayBetweenIterationsInSeconds >= Timeout.Infinite,
                "Delay between iterations must be either Infinite, Zero, or a positive value.");

            services.TryAddSingleton<TWork>();

            ServiceDescriptor? existingWorkBase = services.SingleOrDefault(d =>
                   d.ImplementationType != null
                && d.ImplementationType.IsGenericType
                && d.ImplementationType.GetGenericTypeDefinition() == typeof(WorkHost<,>)
                && d.ImplementationType.GenericTypeArguments.Contains(typeof(TWork)));
            if (existingWorkBase != null)
            {
                // This work is already registered, remove old registration first.
                services.Remove(existingWorkBase);
            }
            services.AddHostedService<WorkHost<TWork, TWorkOptions>>();

            return services;
        }

        internal static IServiceCollection AddWork(this IServiceCollection services, Type workType, IConfiguration? configurationSection = null, Action<IWorkOptions>? configure = null)
        {
            var generic = typeof(ContainerExtensions)
                .GetMethods()
                .Single(m => m.Name == nameof(AddWork) && m.IsGenericMethod);

            var workOptionsType = TypeExtensions.GetWorkOptionsType(workType);

            var concrete = generic.MakeGenericMethod(workType, workOptionsType);
            concrete.Invoke(null, new object?[] { services, configurationSection, configure });
            return services;
        }
    }
}
