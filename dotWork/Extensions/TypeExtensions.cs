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
    }
}
