using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace Julmar.AzDOUtilities
{
    static class ReflectionHelpers
    {
        static readonly Lazy<IReadOnlyDictionary<string, Type>> registeredTypes =
            new Lazy<IReadOnlyDictionary<string, Type>>(() => CollectRegisteredTypes());
        static readonly Lazy<string[]> allFields =
            new Lazy<string[]>(registeredTypes.Value.Values.SelectMany(GetQueryFieldsForType).Distinct().ToArray());

        internal static IReadOnlyDictionary<string, Type> RegisteredTypes => registeredTypes.Value;
        internal static string[] AllFields => allFields.Value;

        internal static string[] GetQueryFieldsForType(Type type)
        {
            return type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy)
                    .Select(prop => prop.GetCustomAttribute<AzDOFieldAttribute>()?.FieldName)
                    .Where(n => n != null)
                    .ToArray();
        }

        static IReadOnlyDictionary<string,Type> CollectRegisteredTypes()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                            .SelectMany(asm => asm.GetCustomAttributes<AzDORegisterAttribute>())
                            .SelectMany(attr => attr.Types)
                            .Where(t => t.GetCustomAttribute<AzDOWorkItemAttribute>() != null)
                            .ToDictionary(type => type.GetCustomAttribute<AzDOWorkItemAttribute>().WorkItemType, type => type);
        }

        internal static IEnumerable<WorkItem> MapWorkItemTypes(IEnumerable<Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem> workItems)
        {
            if (workItems == null)
                yield break;

            foreach (var wi in workItems)
            {
                var wiType = wi.Fields[Field.WorkItemType].ToString();
                if (!RegisteredTypes.TryGetValue(wiType, out var wrapperType))
                    wrapperType = typeof(WorkItem);

                var item = (WorkItem)Activator.CreateInstance(wrapperType);
                item.Initialize(wi);
                yield return item;
            }
        }

    }
}
