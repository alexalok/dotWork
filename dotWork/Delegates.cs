using System.Threading.Tasks;

namespace dotWork
{
    delegate object? InvokeDelegate(object? instance, object?[]? parameters);

    delegate Task ExecuteIterationAsyncDelegate();
}
