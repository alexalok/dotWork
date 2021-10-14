using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace dotWork.Extensions
{
    public static class ContainerExtensions
    {
        /// <summary>
        /// Registers all classes implementing <see cref="IWork"/> interface as works with <see cref="DefaultWorkOptions"/> 
        /// and configures them from <see cref="IConfigurationSection"/>.
        /// <remarks>Only works contained in an assembly that called this method are registered.</remarks>
        /// </summary>
        public static IServiceCollection AddWorks(this IServiceCollection services, IConfigurationSection configurationSection)
        {
            if (configurationSection is null)
                throw new ArgumentNullException(nameof(configurationSection));

            var workTypes = Assembly.GetCallingAssembly().GetTypes().Where(t => t.ImplementsInterface(typeof(IWork)));
            foreach (var workType in workTypes)
            {
                var configSubSection = configurationSection.GetSection(workType.Name);
                AddWork(services, workType, typeof(DefaultWorkOptions), configSubSection);
            }

            return services;
        }

        public static IServiceCollection AddWork<TWork, TWorkOptions>(this IServiceCollection services, IConfigurationSection? configurationSection = null, Action<TWorkOptions>? configure = null)
            where TWork : class
            where TWorkOptions : class, IWorkOptions
        {
            var optionsBuilder = services.AddOptions<TWorkOptions>(typeof(TWork).Name);
            if (configurationSection != null)
                optionsBuilder.Bind(configurationSection);
            if (configure != null)
                optionsBuilder.Configure(configure);

            optionsBuilder.Validate(opt => opt.DelayBetweenIterations >= Timeout.InfiniteTimeSpan,
                "Delay between iterations must be either Infinite, Zero, or a positive TimeSpan value.");

            services.AddSingleton<TWork>();
            services.AddHostedService<WorkBase<TWork, TWorkOptions>>();

            return services;
        }

        internal static IServiceCollection AddWork(this IServiceCollection services, Type workType, Type workOptionsType, IConfigurationSection? configurationSection = null, Action<IWorkOptions>? configure = null)
        {
            var generic = typeof(ContainerExtensions)
                .GetMethods()
                .Single(m => m.Name == nameof(AddWork) && m.IsGenericMethod);
            var concrete = generic.MakeGenericMethod(workType, workOptionsType);
            concrete.Invoke(null, new object?[] { services, configurationSection, configure });
            return services;
        }
    }
}
