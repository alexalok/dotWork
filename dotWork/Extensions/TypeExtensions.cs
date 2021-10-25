using System;
using System.Linq;

namespace dotWork.Extensions
{
    static class TypeExtensions
    {
        public static bool ImplementsInterface(this Type type, Type interfaceType)
        {
            if (interfaceType.IsGenericType)
                return ImplementsGenericInterface(type, interfaceType);
            return type.GetInterfaces().Any(t => t == interfaceType);
        }

        public static bool ImplementsGenericInterface(this Type type, Type interfaceType)
        {
            if (!interfaceType.IsGenericType)
                throw new ArgumentException($"{interfaceType.Name} is not generic.", nameof(interfaceType));
            return type.GetInterfaces().Any(t => t.IsGenericType && t.GetGenericTypeDefinition() == interfaceType);
        }

        public static Type GetWorkOptionsType(Type workType)
        {
            var workInterface = workType.GetInterfaces()
                .SingleOrDefault(i => i.GetGenericTypeDefinition() == typeof(IWork<>));

            if (workInterface == null)
                throw new ArgumentException(nameof(workType), $"{workType.Name} does not implement {typeof(IWork<>).Name} interface.");

            var workOptionsType = workInterface.GetGenericArguments().Single(); // TWorkOptions
            return workOptionsType;
        }
    }
}
