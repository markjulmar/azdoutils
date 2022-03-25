namespace Julmar.AzDOUtilities.Linq;

static class TypeSystem
{
    internal static Type GetElementType(Type seqType)
    {
        var enumerable = FindIEnumerable(seqType);
        return enumerable == null ? seqType : enumerable.GetGenericArguments()[0];
    }

    /// <summary>
    /// Walk a type graph and find the IEnumerable representation if it's present
    /// </summary>
    /// <param name="seqType"></param>
    /// <returns></returns>
    private static Type? FindIEnumerable(Type? seqType)
    {
        if (seqType == null || seqType == typeof(string))
            return null;

        if (seqType.IsArray)
        {
            var type = seqType.GetElementType()!;
            return typeof(IEnumerable<>).MakeGenericType(type);
        }

        if (seqType.IsGenericType)
        {
            foreach (var arg in seqType.GetGenericArguments())
            {
                var enumerable = typeof(IEnumerable<>).MakeGenericType(arg);
                if (enumerable.IsAssignableFrom(seqType))
                {
                    return enumerable;
                }
            }
        }

        Type[] interfaces = seqType.GetInterfaces();
        if (interfaces.Length > 0)
        {
            foreach (var interfaceType in interfaces)
            {
                var enumerable = FindIEnumerable(interfaceType);
                if (enumerable != null) 
                    return enumerable;
            }
        }

        if (seqType.BaseType != null && seqType.BaseType != typeof(object))
        {
            return FindIEnumerable(seqType.BaseType);
        }

        return null;
    }
}