using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace dotWork.Extensions
{
    public static class ContainerExtensions
    {
        public static IServiceCollection AddWork<TWork, TWorkOptions>(this IServiceCollection services, Action<TWorkOptions>? configure = null)
            where TWork : class
            where TWorkOptions : class, IWorkOptions
        {
            var optionsBuilder = services.AddOptions<TWorkOptions>(typeof(TWork).Name);
            if (configure != null)
                optionsBuilder.Configure(configure);

            services.AddSingleton<TWork>();
            services.AddHostedService<WorkBase<TWork, TWorkOptions>>();

            return services;
        }

        internal static IServiceCollection AddWork(this IServiceCollection services, Type workType, Type workOptionsType, Action<IWorkOptions>? configure = null)
        {
            var generic = typeof(ContainerExtensions)
                .GetMethods()
                .Single(m => m.Name == nameof(AddWork) && m.IsGenericMethod);
            var concrete = generic.MakeGenericMethod(workType, workOptionsType);
            concrete.Invoke(null, new object?[] { services, configure });
            return services;
        }
    }
}
