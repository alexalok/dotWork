using System.Reflection;

namespace dotWork
{
    class IterationMethodMetadata
    {
        public InvokeDelegate Invoke { get; }

        public ParameterInfo[] Parameters { get; }

        public bool IsAsync { get; set; }

        public IterationMethodMetadata(InvokeDelegate invoke, ParameterInfo[] parameters, bool isAsync)
        {
            Invoke = invoke;
            Parameters = parameters;
            IsAsync = isAsync;
        }
    }
}
