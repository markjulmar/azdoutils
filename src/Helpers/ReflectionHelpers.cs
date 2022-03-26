using System.Collections;
using System.Reflection;

using Wit = Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem;

namespace Julmar.AzDOUtilities;

/// <summary>
/// Helper methods to leverage the Reflection API to get and set property values
/// </summary>
internal static class ReflectionHelpers
{
    private static readonly Lazy<IReadOnlyDictionary<string, Type>> registeredTypes = new(CollectRegisteredTypes);
    private static string[]? allFields;

    /// <summary>
    /// Retrieve all the known types we support.
    /// </summary>
    internal static IReadOnlyDictionary<string, Type> RegisteredTypes => registeredTypes.Value;

    /// <summary>
    /// Retrieve all the fields for the Azure DevOps instance we're connected to.
    /// </summary>
    /// <param name="service">Service wrapper</param>
    /// <returns>String array with all the standard/custom field names</returns>
    internal static string[] GetAllFields(AzDOService service)
    {
        if (allFields == null)
        {
            var fields = registeredTypes.Value.Values.SelectMany(GetQueryFieldsForType).Distinct().ToArray();
            var definedFields = service.WorkItemClient.GetFieldsAsync().Result.ToList();

#if DEBUG
            foreach (var badField in fields.Where(f => definedFields.All(df => df.ReferenceName != f)))
                service.log?.WriteLine(LogLevel.Query, $"Field \"{badField}\" is not defined in AzDO and will be skipped.");
#endif

            allFields = fields.Where(f => definedFields.Any(df => df.ReferenceName == f)).ToArray();
        }

        return allFields;
    }

    /// <summary>
    /// Return all the supported fields from this Azure DevOps instances filtered by a list.
    /// </summary>
    /// <param name="filterToFields"></param>
    /// <param name="service"></param>
    /// <returns></returns>
    internal static IEnumerable<string> AvailableFields(this IEnumerable<string> filterToFields, AzDOService service)
    {
        var availableFields = GetAllFields(service);
        return filterToFields.Where(f => Array.IndexOf(availableFields, f) >= 0);
    }

    /// <summary>
    /// Get all fields mapped to a specific type
    /// </summary>
    /// <param name="type">Type to look for</param>
    /// <returns>Array of fields</returns>
    internal static string[] GetQueryFieldsForType(Type type)
    {
        return type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy)
            .Select(prop => prop.GetCustomAttribute<AzDOFieldAttribute>()?.FieldName)
            .Where(n => n != null)
            .Cast<string>()
            .ToArray();
    }

    /// <summary>
    /// Get the field attributes for a specific type
    /// </summary>
    /// <param name="type">Type to look for</param>
    /// <returns>Property information</returns>
    internal static (PropertyInfo Property,AzDOFieldAttribute? Field)[] GetFieldAttributesForType(Type type)
    {
        return type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy)
            .Select(prop => (Property: prop, Field: prop.GetCustomAttribute<AzDOFieldAttribute>()))
            .Where(n => n.Field != null)
            .ToArray();
    }

    /// <summary>
    /// Get all the registered custom types tied to this executable.
    /// </summary>
    /// <returns>Names and types</returns>
    private static IReadOnlyDictionary<string,Type> CollectRegisteredTypes()
    {
        // Get all registered types from this assembly first. These are the core
        // types we'll use. Other assemblies can replace those types.
        var thisAssembly = Assembly.GetExecutingAssembly();
        var baseDictionary = thisAssembly
            .GetCustomAttributes<AzDORegisterAttribute>()
            .SelectMany(attr => attr.Types)
            .Where(t => t.GetCustomAttribute<AzDOWorkItemAttribute>() != null)
            .ToDictionary(type => type.GetCustomAttribute<AzDOWorkItemAttribute>()!.WorkItemType, type => type);

        // Layer in additional assemblies.
        foreach (var type in AppDomain.CurrentDomain.GetAssemblies()
                     .Where(asm => asm != thisAssembly)
                     .SelectMany(asm => asm.GetCustomAttributes<AzDORegisterAttribute>())
                     .SelectMany(attr => attr.Types)
                     .Where(t => t.GetCustomAttribute<AzDOWorkItemAttribute>() != null))
        {
            var key = type.GetCustomAttribute<AzDOWorkItemAttribute>()!.WorkItemType;
            baseDictionary[key] = type;
        }

        return baseDictionary;
    }

    /// <summary>
    /// Creates a new WorkItem from a Wit
    /// </summary>
    /// <typeparam name="T">WorkItem type to create</typeparam>
    /// <param name="workItem">AzureDevOps wire type</param>
    /// <returns>New WorkItem wrapper</returns>
    internal static T FromWorkItem<T>(Wit workItem) where T : WorkItem, new()
    {
        var rc = new T();
        rc.Initialize(workItem);
        return rc;
    }

    /// <summary>
    /// Maps a set of Azure DevOps Wits to custom Work Item objects.
    /// </summary>
    /// <param name="workItems">Azure DevOps wire types</param>
    /// <returns>Work Item wrappers</returns>
    internal static IEnumerable<WorkItem> MapWorkItemTypes(IEnumerable<Wit> workItems)
    {
        foreach (var wi in workItems)
        {
            var wiType = wi.Fields[Field.WorkItemType].ToString();
            if (!RegisteredTypes.TryGetValue(wiType!, out var wrapperType))
                wrapperType = typeof(WorkItem);

            var item = (WorkItem?) Activator.CreateInstance(wrapperType);
            if (item != null)
            {
                item.Initialize(wi);
                yield return item;
            }
        }
    }

    /// <summary>
    /// Compare two fields
    /// </summary>
    /// <param name="initialValue"></param>
    /// <param name="newValue"></param>
    /// <returns></returns>
    internal static bool CompareField(object? initialValue, object? newValue)
    {
        if (initialValue == null && newValue == null) return true;
        if (initialValue == null || newValue == null)
        {
            // Allow null == string.Empty
            if (initialValue?.GetType() == typeof(string))
                newValue = string.Empty;
            else if (newValue?.GetType() == typeof(string))
                initialValue = string.Empty;
            else
                return false;
        }

        var initialType = initialValue.GetType();
        var newType = newValue.GetType();

        if (initialType == newType)
        {
            if (initialValue is IList list1)
            {
                var list2 = (IList)newValue;
                return list1.Count == list2.Count 
                       && list1.Cast<object>().Where((t, i) => !Equals(t, list2[i])).Any() == false;
            }
            return Equals(initialValue, newValue);
        }

        if (newType == typeof(string))
        {
            return string.CompareOrdinal(newValue.ToString(), initialValue.ToString()) == 0;
        }

        if (newType.IsGenericType &&
            newType.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            return Equals(Convert.ChangeType(initialValue, newType.GetGenericArguments()[0]), newValue);
        }

        return Equals(Convert.ChangeType(initialValue, newType), newValue);
    }
}