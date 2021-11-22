using System.Reflection;

namespace dotWork
{
    class IterationMethodMetadata
    {
        public InvokeDelegate Invoke { get; }

        public ParameterInfo[] Parameters { get; }

        public bool IsAsync { get; set; }

        /// <summary>
        ///     For async works, true if return value is ValueTask, false if it's Task. Always false for non-async works.
        /// </summary>
        public bool IsValueTask { get; }

        public IterationMethodMetadata(InvokeDelegate invoke, ParameterInfo[] parameters, bool isAsync,
            bool isValueTask)
        {
            Invoke = invoke;
            Parameters = parameters;
            IsAsync = isAsync;
            IsValueTask = isValueTask;
        }
    }
}
