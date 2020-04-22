using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Wit = Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem;

namespace Julmar.AzDOUtilities
{
    static class ReflectionHelpers
    {
        static readonly Lazy<IReadOnlyDictionary<string, Type>> registeredTypes =
                                new Lazy<IReadOnlyDictionary<string, Type>>(() => CollectRegisteredTypes());
        static string[] allFields;

        internal static IReadOnlyDictionary<string, Type> RegisteredTypes => registeredTypes.Value;

        internal static string[] GetAllFields(AzDOService service)
        {
            if (allFields == null)
            {
                var fields = registeredTypes.Value.Values.SelectMany(GetQueryFieldsForType).Distinct().ToArray();
                var definedFields = service.WorkItemClient.GetFieldsAsync().Result.ToList();

#if DEBUG
                foreach (var badField in fields.Where(f => !definedFields.Any(df => df.ReferenceName == f)))
                {
                    service.log.WriteLine(LogLevel.Query, $"Field \"{badField}\" is not defined in AzDO and will be skipped.");
                }
#endif

                allFields = fields.Where(f => definedFields.Any(df => df.ReferenceName == f)).ToArray();
            }

            return allFields;
        }

        internal static IEnumerable<string> FilterFields(this IEnumerable<string> fields, AzDOService service)
        {
            var allFields = GetAllFields(service);
            return fields.Where(f => Array.IndexOf(allFields, f) >= 0);
        }

        internal static string[] GetQueryFieldsForType(Type type)
        {
            return type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy)
                    .Select(prop => prop.GetCustomAttribute<AzDOFieldAttribute>()?.FieldName)
                    .Where(n => n != null)
                    .ToArray();
        }

        internal static (PropertyInfo Property,AzDOFieldAttribute Field)[] GetFieldAttributesForType(Type type)
        {
            return type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy)
                    .Select(prop => (Property: prop, Field: prop.GetCustomAttribute<AzDOFieldAttribute>()))
                    .Where(n => n.Field != null)
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

        internal static IEnumerable<WorkItem> MapWorkItemTypes(IEnumerable<Wit> workItems)
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

        /// <summary>
        /// Compare two fields
        /// </summary>
        /// <param name="initialValue"></param>
        /// <param name="newValue"></param>
        /// <returns></returns>
        internal static bool CompareField(object initialValue, object newValue)
        {
            if (initialValue == null && newValue == null)
                return true;
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

            Type initialType = initialValue.GetType();
            Type newType = newValue.GetType();

            if (initialType == newType)
            {
                if (initialValue is IList list1)
                {
                    IList list2 = (IList)newValue;

                    if (list1.Count != list2.Count) return false;
                    for (int i = 0; i < list1.Count; i++)
                    {
                        if (!object.Equals(list1[i], list2[i]))
                            return false;
                    }
                    return true;
                }
                else
                {
                    return object.Equals(initialValue, newValue);
                }
            }
            else if (newType == typeof(string))
            {
                return string.Compare(newValue.ToString(), initialValue.ToString()) == 0;
            }
            else if (newType.IsGenericType &&
                     newType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                object tv = Convert.ChangeType(initialValue, newType.GetGenericArguments()[0]);
                return object.Equals(tv, newValue);
            }

            object testValue = Convert.ChangeType(initialValue, newType);
            return object.Equals(testValue, newValue);
        }
    }
}
