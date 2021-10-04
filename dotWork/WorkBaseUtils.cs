using System;
using dotWork.Extensions;

namespace dotWork
{
    class WorkBaseUtils
    {
        public static Type MakeGenericType(Type workType, Type workOptionsType)
        {
            if (!workOptionsType.ImplementsInterface(typeof(IWorkOptions)))
                throw new ArgumentException($"{nameof(workOptionsType.Name)} does not implement IWorkOptions interface.", nameof(workOptionsType));

            var workBaseType = typeof(WorkBase<,>).MakeGenericType(workType, workOptionsType);
            return workBaseType;
        }


    }
}
