using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

using WitModel = Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem;

namespace AzDOUtilities
{
    public class WorkItem
    {
        private WitModel workItem;

        public int? Id => workItem?.Id;
        public int? Revision => workItem?.Rev;
        public string Url => workItem?.Url;

        [AzDOField(Field.ChangedDate, IsReadOnly = true)]
        public DateTime? ChangedDate { get; protected set; }
        [AzDOField(Field.StateChangedDate, IsReadOnly = true)]
        public DateTime? StateChangedDate { get; protected set; }
        [AzDOField(Field.CreatedBy, IsReadOnly = true)]
        public string CreatedBy { get; protected set; }
        [AzDOField(Field.ChangedBy, IsReadOnly = true, Converter = typeof(IdentityRefConverter))]
        public string ChangedBy { get; protected set; }
        [AzDOField(Field.CreatedDate, IsReadOnly = true)]
        public DateTime? CreatedDate { get; protected set; }
        [AzDOField(Field.ClosedBy, Converter = typeof(IdentityRefConverter))]
        public string ClosedBy { get; protected set; }
        [AzDOField(Field.ClosedDate)]
        public DateTime? ClosedDate { get; protected set; }

        [AzDOField(Field.WorkItemType)]
        public string WorkItemType { get; protected set; }

        [AzDOField(Field.Project)]
        public string Project { get; set; }

        [AzDOField(Field.History, IsReadOnly = true)]
        public string History { get; protected set; }

        [AzDOField(Field.AreaPath)]
        public string AreaPath { get; set; }
        [AzDOField(Field.AreaId)]
        public int? AreaId { get; set; }
        [AzDOField(Field.IterationPath)]
        public string IterationPath { get; set; }
        [AzDOField(Field.IterationId)]
        public int? IterationId { get; set; }
        [AzDOField(Field.StackRank)]
        public double? StackRank { get; set; }

        [AzDOField(Field.Title)]
        public string Title { get; set; }
        [AzDOField(Field.Description)]
        public string Description { get; set; }
        [AzDOField(Field.State)]
        public string State { get; set; }
        [AzDOField(Field.Reason)]
        public string Reason { get; set; }
        [AzDOField(Field.Priority)]
        public int? Priority { get; set; }
        [AzDOField(Field.AssignedTo, Converter = typeof(IdentityRefConverter))]
        public string AssignedTo { get; set; }
        [AzDOField(Field.AcceptedBy, Converter = typeof(IdentityRefConverter))]
        public string AcceptedBy { get; set; }
        [AzDOField(Field.Tags, Converter = typeof(SemicolonSeparatedConverter))]
        public List<string> Tags { get; set; }

        public bool IsNew => workItem == null;

        internal void Initialize(WitModel workItem)
        {
            if (workItem == null)
                throw new ArgumentNullException(nameof(workItem));

            if (this.workItem != null
                && this.workItem.Id != workItem.Id)
            {
                throw new ArgumentException("WorkItem Id mismatch", nameof(workItem));
            }

            this.workItem = workItem;
            this.ResetToInitialState();
        }

        protected internal bool TryGetInitialValue<T>(string fieldName, out T value)
        {
            object field = null;
            if (workItem?.Fields.TryGetValue(fieldName, out field) == true)
            {
                value = (T) field;
                return true;
            }

            value = default;
            return false;
        }

        public bool HasChanges
        {
            get
            {
                bool isChanged = false;
                ProcessFieldProperties((_,__,___, changed) => { isChanged = changed; return !changed; /*stop on 1st change*/ });
                return isChanged;
            }
        }

        internal JsonPatchDocument CreatePatchDocument()
        {
            var document = new JsonPatchDocument();

            ProcessFieldProperties((fieldInfo, initialValue, currentValue, changed) =>
            {
                if (changed)
                {
                    JsonPatchOperation operation;
                    if (currentValue == null)
                    {
                        operation = new JsonPatchOperation
                        {
                            Operation = Operation.Remove,
                            Path = $"/fields/{fieldInfo.FieldName}"
                        };
                    }
                    else if (initialValue == null)
                    {
                        operation = new JsonPatchOperation
                        {
                            Operation = Operation.Add,
                            Path = $"/fields/{fieldInfo.FieldName}",
                            Value = currentValue.ToString()
                        };
                    }
                    else
                    {
                        operation = new JsonPatchOperation
                        {
                            Operation = Operation.Replace,
                            Path = $"/fields/{fieldInfo.FieldName}",
                            Value = currentValue.ToString()
                        };
                    }
                    document.Add(operation);
                }
                return true;
            });

            if (document.Count > 0)
            {
                if (workItem != null)
                {
                    document.Insert(0, new JsonPatchOperation
                    {
                        Operation = Operation.Test,
                        Path = "/rev",
                        Value = Revision
                    });
                }
                return document;
            }

            return null;
        }

        private bool CompareField(object initialValue, object newValue)
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

        protected void ProcessFieldProperties(Func<AzDOFieldAttribute,object,object,bool,bool> processor)
        {
            if (processor == null)
                throw new ArgumentNullException(nameof(processor));

            foreach (var prop in GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy))
            {
                var fieldInfo = prop.GetCustomAttribute<AzDOFieldAttribute>();
                if (fieldInfo?.IsReadOnly == false)
                {
                    object currentValue = prop.GetValue(this);
                    TryGetInitialValue(fieldInfo.FieldName, out object initialValue);

                    bool? changed = null;
                    if (fieldInfo.Converter != null)
                    {
                        var converter = (IFieldConverter)Activator.CreateInstance(fieldInfo.Converter);
                        if (converter is IFieldComparer comparer)
                        {
                            changed = !comparer.Compare(initialValue, currentValue);
                        }

                        currentValue = converter.ConvertBack(currentValue);
                    }

                    if (currentValue != null && (currentValue.GetType() == typeof(DateTime) || currentValue.GetType() == typeof(DateTime?)))
                    {
                        // Azure DevOps is always in UTC.
                        currentValue = ((DateTime)currentValue).ToUniversalTime();
                    }

                    if (changed == null)
                    {
                        changed = !CompareField(currentValue, initialValue);
                    }

                    if (!processor.Invoke(fieldInfo, initialValue, currentValue, changed.Value))
                    {
                        // Don't continue.
                        break;
                    }
                }
            }
        }

        public IReadOnlyList<(string FieldName, string OldValue, string NewValue)> GatherChangeList()
        {
            var changes = new List<(string FieldName, string OldValue, string NewValue)>();
            ProcessFieldProperties((fieldInfo, initialValue, currentValue, changed) =>
            {
                if (changed)
                {
                    changes.Add((fieldInfo.FieldName,
                        initialValue == null ? "(null)": initialValue.ToString(),
                        currentValue == null ? "(null)" : currentValue.ToString()));
                }
                return true;
            });
            return changes;
        }

        public string ToDeltaString(bool full = false)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"{WorkItemType} {Id}");

            ProcessFieldProperties((fieldInfo, initialValue, currentValue, changed) =>
            {
                if (!changed)
                {
                    if (full)
                        sb.AppendLine($"{fieldInfo.FieldName}: {currentValue}");
                }
                else
                {
                    sb.AppendLine($"*{fieldInfo.FieldName}: {currentValue} ({initialValue})");
                }
                return true;
            });

            return sb.ToString();
        }

        public void ResetToInitialState()
        {
            foreach (var prop in GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy)
                                          .Where(p => p.CanWrite))
            {
                var fieldInfo = prop.GetCustomAttribute<AzDOFieldAttribute>();
                if (fieldInfo != null)
                {
                    TryGetInitialValue(fieldInfo.FieldName, out object initialValue);

                    // Always run the value through the converter - even if null as the converter
                    // can change the value.
                    if(fieldInfo.Converter != null)
                    {
                        initialValue = ((IFieldConverter)Activator.CreateInstance(fieldInfo.Converter))
                            .Convert(initialValue, prop.PropertyType);
                    }

                    // Assign the value to the local property.
                    if (initialValue == null || prop.PropertyType == initialValue.GetType())
                    {
                        prop.SetValue(this, initialValue);
                    }
                    else if (prop.PropertyType == typeof(string))
                    {
                        prop.SetValue(this, initialValue?.ToString());
                    }
                    else if (prop.PropertyType == typeof(DateTime) || prop.PropertyType == typeof(DateTime?))
                    {
                        // Dates in VSTS are always UTC.
                        if (initialValue == null)
                        {
                            if (prop.PropertyType == typeof(DateTime?))
                                prop.SetValue(this, null);
                            else
                                throw new Exception($"Cannot serialize null value into {prop.PropertyType.Name} {prop.Name}");
                        }
                        else if(initialValue != null)
                        {
                            DateTime utcDate = (DateTime)initialValue;
                            prop.SetValue(this, utcDate.ToLocalTime());
                        }
                    }
                    else if (prop.PropertyType.IsGenericType &&
                                prop.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        prop.SetValue(this, Convert.ChangeType(initialValue, prop.PropertyType.GetGenericArguments()[0]));
                    }
                    else
                    {
                        prop.SetValue(this, Convert.ChangeType(initialValue, prop.PropertyType));
                    }
                }
            }
        }

        public void ChangeType(string newWitType)
        {
            this.WorkItemType = newWitType;
        }

        public override string ToString()
        {
            return $"{WorkItemType} {Id}: ({State}) \"{Title}\"";
        }

        internal static T FromWorkItem<T>(WitModel workItem) where T : WorkItem, new()
        {
            var rc = new T();
            rc.Initialize(workItem);
            return rc;
        }
    }
}
