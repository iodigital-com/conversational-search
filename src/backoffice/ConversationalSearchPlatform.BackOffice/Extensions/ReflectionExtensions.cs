using System.Reflection;

namespace ConversationalSearchPlatform.BackOffice.Extensions;

public static class ReflectionExtensions
{
    public static object? InvokeMethod<T>(this T obj, string methodName, params object[] args)
    {
        var type = typeof(T);
        var method = type.GetTypeInfo().GetDeclaredMethod(methodName);
        return method?.Invoke(obj, args);
    }
}