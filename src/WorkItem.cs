using System.Diagnostics;
using System.Reflection;
using System.Text;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

using WitModel = Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem;

namespace Julmar.AzDOUtilities;

/// <summary>
/// Base WorkItem type that wraps a WorkItem in Azure DevOps to support
/// change tracking and property mapping.
/// </summary>
[DebuggerDisplay("[{Id}] {WorkItemType} - {Title}")]
public class WorkItem : IEquatable<WorkItem>
{
    private WitModel? witModel;
    private StringBuilder? addComments;

    /// <summary>
    /// Constructor
    /// </summary>
    public WorkItem()
    {
        WorkItemType = GetWorkItemType(GetType());
    }

    /// <summary>
    /// Fields we loaded from the WIT
    /// </summary>
    public IEnumerable<string> ValidFields => witModel?.Fields.Keys ?? Enumerable.Empty<string>();

    /// <summary>
    /// Unique id for the WorkItem. Null if not created or mapped to AzDO.
    /// </summary>
    public int? Id => witModel?.Id;

    /// <summary>
    /// Current revision for the WorkItem
    /// </summary>
    public int? Revision => witModel?.Rev;

    /// <summary>
    /// Date this work item was revised.
    /// </summary>
    [AzDOField(Field.RevisedDate, IsReadOnly = true)]
    public DateTime? RevisedDate { get; protected set; }

    /// <summary>
    /// Relations of the work item.
    /// </summary>
    public IList<WorkItemRelation> Relations => witModel?.Relations ?? new List<WorkItemRelation>();

    /// <summary>
    /// URL for the WorkItem in Azure DevOps.
    /// </summary>
    public string Url => witModel?.Url ?? string.Empty;

    /// <summary>
    /// True if this is a new WorkItem that is not on the server.
    /// </summary>
    public bool IsNew => witModel == null || Id == null;

    /// <summary>
    /// Associated parent id
    /// </summary>
    [AzDOField(Field.Parent)]
    public int? ParentId { get; protected set; }

    /// <summary>
    /// Activated date for this work item
    /// </summary>
    [AzDOField(Field.ActivatedDate, IsReadOnly = true)]
    public DateTime? ActivatedDate { get; protected set; }

    /// <summary>
    /// Person who activated this work item
    /// </summary>
    [AzDOField(Field.ActivatedBy, Converter = typeof(IdentityRefConverter))]
    public string? ActivatedBy { get; set; }

    /// <summary>
    /// Authorization date for this work item
    /// </summary>
    [AzDOField(Field.AuthorizedDate, IsReadOnly = true)]
    public DateTime? AuthorizedDate { get; protected set; }

    /// <summary>
    /// Person who authorized this work item
    /// </summary>
    [AzDOField(Field.AuthorizedAs, Converter = typeof(IdentityRefConverter))]
    public string? AuthorizedAs { get; set; }

    /// <summary>
    /// Changed date for this work item
    /// </summary>
    [AzDOField(Field.ChangedDate, IsReadOnly = true)]
    public DateTime? ChangedDate { get; protected set; }

    /// <summary>
    /// Board column for this work item.
    /// </summary>
    [AzDOField(Field.BoardColumn)]
    public string? BoardColumn { get; set; }

    /// <summary>
    /// True if this is done.
    /// </summary>
    [AzDOField(Field.BoardColumnDone)]
    public bool? BoardColumnDone { get; set; }

    /// <summary>
    /// Board lane for this work item.
    /// </summary>
    [AzDOField(Field.BoardLane)]
    public string? BoardLane { get; set; }

    /// <summary>
    /// State changed date
    /// </summary>
    [AzDOField(Field.StateChangedDate, IsReadOnly = true)]
    public DateTime? StateChangedDate { get; protected set; }

    /// <summary>
    /// Person who created this Work Item
    /// </summary>
    [AzDOField(Field.CreatedBy, Converter = typeof(IdentityRefConverter))]
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Last person who changed this Work Item
    /// </summary>
    [AzDOField(Field.ChangedBy, Converter = typeof(IdentityRefConverter))]
    public string? ChangedBy { get; set; }

    /// <summary>
    /// The date this Work Item was created on
    /// </summary>
    [AzDOField(Field.CreatedDate, IsReadOnly = true)]
    public DateTime? CreatedDate { get; protected set; }

    /// <summary>
    /// The person who closed this Work item. Empty if not closed.
    /// </summary>
    [AzDOField(Field.ClosedBy, Converter = typeof(IdentityRefConverter))]
    public string? ClosedBy { get; set; }

    /// <summary>
    /// Date this work item was closed.
    /// </summary>
    [AzDOField(Field.ClosedDate, IsReadOnly = true)]
    public DateTime? ClosedDate { get; protected set; }

    /// <summary>
    /// Number of comments associated to this work item.
    /// </summary>
    [AzDOField(Field.CommentCount)]
    public int? CommentCount { get; protected set; }

    /// <summary>
    /// Number of related links tied to this work item.
    /// </summary>
    [AzDOField(Field.RelatedLinkCount)]
    public int? RelatedLinkCount { get; protected set; }

    /// <summary>
    /// Number of external links tied to this work item.
    /// </summary>
    [AzDOField(Field.ExternalLinkCount)]
    public int? ExternalLinkCount { get; protected set; }

    /// <summary>
    /// Number of hyperlinks tied to this work item.
    /// </summary>
    [AzDOField(Field.HyperLinkCount)]
    public int? HyperLinkCount { get; protected set; }

    /// <summary>
    /// Number of files attached to this work item.
    /// </summary>
    [AzDOField(Field.AttachedFileCount)]
    public int? AttachedFileCount { get; protected set; }

    /// <summary>
    /// Number of remote links tied to this work item.
    /// </summary>
    [AzDOField(Field.RemoteLinkCount)]
    public int? RemoteLinkCount { get; protected set; }

    /// <summary>
    /// Node name
    /// </summary>
    [AzDOField(Field.NodeName)]
    public string? NodeName { get; protected set; }

    /// <summary>
    /// Resolved date for this work item
    /// </summary>
    [AzDOField(Field.ResolvedDate, IsReadOnly = true)]
    public DateTime? ResolvedDate { get; protected set; }

    /// <summary>
    /// Person who resolved this Work Item
    /// </summary>
    [AzDOField(Field.ResolvedBy, Converter = typeof(IdentityRefConverter))]
    public string? ResolvedBy { get; set; }

    /// <summary>
    /// Resolved reason
    /// </summary>
    [AzDOField(Field.ResolvedReason)]
    public string? ResolvedReason { get; set; }

    /// <summary>
    /// Work item type
    /// </summary>
    [AzDOField(Field.WorkItemType)]
    public string WorkItemType { get; protected set; }

    /// <summary>
    /// Project this work item is tied to
    /// </summary>
    [AzDOField(Field.Project)] 
    public string? Project { get; set; }

    /// <summary>
    /// History for this work item
    /// </summary>
    [AzDOField(Field.History, IsReadOnly = true)]
    public string? History { get; protected set; }

    /// <summary>
    /// Area path for this work item
    /// </summary>
    [AzDOField(Field.AreaPath)]
    public string? AreaPath { get; set; }

    /// <summary>
    /// Area id for this work item
    /// </summary>
    [AzDOField(Field.AreaId)]
    public int? AreaId { get; set; }

    /// <summary>
    /// Iteration path for this work item
    /// </summary>
    [AzDOField(Field.IterationPath)]
    public string?IterationPath { get; set; }

    /// <summary>
    /// Iteration identifier
    /// </summary>
    [AzDOField(Field.IterationId)]
    public int? IterationId { get; set; }

    /// <summary>
    /// Stack rank for this item
    /// </summary>
    [AzDOField(Field.StackRank)]
    public double? StackRank { get; set; }

    /// <summary>
    /// Title for this work item
    /// </summary>
    [AzDOField(Field.Title)]
    public string? Title { get; set; }

    /// <summary>
    /// Description for this work item
    /// </summary>
    [AzDOField(Field.Description)]
    public string? Description { get; set; }

    /// <summary>
    /// State of the item
    /// </summary>
    [AzDOField(Field.State)]
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// Reason for the state
    /// </summary>
    [AzDOField(Field.Reason)]
    public string? Reason { get; set; }

    /// <summary>
    /// Work item priority
    /// </summary>
    [AzDOField(Field.Priority)]
    public int? Priority { get; set; }

    /// <summary>
    /// Who this work item is assigned to
    /// </summary>
    [AzDOField(Field.AssignedTo, Converter = typeof(IdentityRefConverter))]
    public string? AssignedTo { get; set; }

    /// <summary>
    /// Who accepted this work item
    /// </summary>
    [AzDOField(Field.AcceptedBy, Converter = typeof(IdentityRefConverter))]
    public string? AcceptedBy { get; set; }

    /// <summary>
    /// Watermark.
    /// </summary>
    [AzDOField(Field.Watermark)]
    public int? Watermark { get; set; }

    /// <summary>
    /// Optional tags for this work item
    /// </summary>
    [AzDOField(Field.Tags, Converter = typeof(SemicolonSeparatedConverter))]
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Initialize a wrapper object from a AzDO WorkItem
    /// </summary>
    /// <param name="workItem">Azure DevOps WorkItem</param>
    internal void Initialize(WitModel workItem)
    {
        if (workItem == null)
            throw new ArgumentNullException(nameof(workItem));

        if (this.witModel != null
            && this.witModel.Id != workItem.Id)
            throw new ArgumentException("WorkItem Id mismatch", nameof(workItem));

        if (!object.ReferenceEquals(workItem, this.witModel))
            this.witModel = workItem;

        this.ResetToInitialState();
    }

    /// <summary>
    /// Retrieve the initial value for a field
    /// </summary>
    /// <typeparam name="T">Type of the field</typeparam>
    /// <param name="fieldName">Field ReferenceName in Azure DevOps</param>
    /// <param name="value">Output value</param>
    /// <returns>True if the field value is set, false if not.</returns>
    public bool TryGetFieldValue<T>(string fieldName, out T? value)
    {
        if (witModel?.Fields.TryGetValue(fieldName, out var field) == true) 
        {
            value = (T?) field;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// True if this WorkItem has unsaved changes.
    /// </summary>
    public bool HasChanges
    {
        get
        {
            bool isChanged = addComments != null;
            if(!isChanged)
            {
                ProcessFieldProperties((_, __, ___, changed) =>
                {
                    isChanged = changed; return !changed; /*stop on 1st change*/
                });
            }
            return isChanged;
        }
    }

    /// <summary>
    /// Creates a JSON PatchDocument to update the state of the server-side WorkItem.
    /// </summary>
    /// <returns>JSON document</returns>
    internal JsonPatchDocument? CreatePatchDocument()
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

        if (addComments != null)
        {
            document.Add(new JsonPatchOperation
            {
                Operation = Operation.Add,
                Path = $"/fields/System.History",
                Value = addComments.ToString()
            });
        }

        if (document.Count > 0)
        {
            if (witModel != null)
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

    /// <summary>
    /// Spins through all the mapped properties/fields and calls a delegate to process each one.
    /// This is used to generate Patch documents, debug output, and determine changes.
    /// </summary>
    /// <param name="processor">Processor for each field.</param>
    protected void ProcessFieldProperties(Func<AzDOFieldAttribute,object?,object?,bool,bool> processor)
    {
        if (processor == null)
            throw new ArgumentNullException(nameof(processor));

        foreach (var prop in GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy))
        {
            var fieldInfo = prop.GetCustomAttribute<AzDOFieldAttribute>();
            if (fieldInfo?.IsReadOnly == false)
            {
                object? currentValue = prop.GetValue(this);
                TryGetFieldValue(fieldInfo.FieldName, out object? initialValue);

                bool? changed = null;
                if (fieldInfo.Converter != null)
                {
                    var converter = Activator.CreateInstance(fieldInfo.Converter) as IFieldConverter;
                    if (converter == null) 
                        throw new InvalidOperationException($"Field converter {fieldInfo.Converter.Name} couldn't be created.");
                    if (converter is IFieldComparer comparer)
                        changed = !comparer.Compare(initialValue, currentValue);

                    currentValue = converter.ConvertBack(currentValue);
                }

                if (currentValue is DateTime dt)
                {
                    // Azure DevOps is always in UTC.
                    currentValue = dt.ToUniversalTime();
                }

                if (changed == null)
                {
                    try
                    {
                        changed = !ReflectionHelpers.CompareField(currentValue, initialValue);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Failed to compare {prop.PropertyType.Name} {prop.Name} ({prop.GetValue(this)} to {fieldInfo.FieldName} ({initialValue})", ex);
                    }
                }

                if (!processor.Invoke(fieldInfo, initialValue, currentValue, changed.Value))
                {
                    // Don't continue.
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Returns the set of fields that have been changed since this WorkItem was retrieved
    /// from the server.
    /// </summary>
    /// <returns>Tuple collection of changes.</returns>
    public IReadOnlyList<(string FieldName, string OldValue, string NewValue)> GatherChangeList()
    {
        var changes = new List<(string FieldName, string OldValue, string NewValue)>();
        ProcessFieldProperties((fieldInfo, initialValue, currentValue, changed) =>
        {
            if (changed)
            {
                changes.Add((fieldInfo.FieldName,
                    initialValue?.ToString()?? "(null)",
                    currentValue?.ToString()?? "(null)"));
            }
            return true;
        });
        return changes;
    }

    /// <summary>
    /// This resets the WorkItem state back to the initial server-side state.
    /// </summary>
    public void ResetToInitialState()
    {
        addComments = null;

        foreach (var prop in GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy)
                     .Where(p => p.CanWrite))
        {
            var fieldInfo = prop.GetCustomAttribute<AzDOFieldAttribute>();
            if (fieldInfo != null)
            {
                bool foundValue = TryGetFieldValue(fieldInfo.FieldName, out object? initialValue);

                try
                {
                    // Always run the value through the converter - even if null as the converter
                    // can change the value.
                    if (fieldInfo.Converter != null)
                    {
                        var converter = Activator.CreateInstance(fieldInfo.Converter) as IFieldConverter;
                        if (converter == null)
                            throw new InvalidOperationException(
                                $"Failed to create converter {fieldInfo.Converter.Name}.");
                        var newValue = converter.Convert(initialValue, prop.PropertyType);
                        initialValue = newValue;
                    }

                    // Assign the value to the local property.
                    if (initialValue == null || prop.PropertyType == initialValue.GetType())
                    {
                        // Don't replace WorkItemType.
                        if (prop.Name != nameof(WorkItemType) || initialValue != null)
                            prop.SetValue(this, initialValue);
                    }
                    else if (prop.PropertyType == typeof(string))
                    {
                        prop.SetValue(this, initialValue.ToString());
                    }
                    else if (prop.PropertyType == typeof(DateTime) || prop.PropertyType == typeof(DateTime?))
                    {
                        // Dates in VSTS are always UTC.
                        DateTime utcDate = (DateTime) initialValue;
                        prop.SetValue(this, utcDate.ToLocalTime());
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
                catch (Exception ex)
                {
                    string ivalue = foundValue
                        ? initialValue != null
                            ? $"{initialValue.GetType().Name} {initialValue}"
                            : "null"
                        : "{none}";
                    throw new Exception($"Failed to set {prop.PropertyType.Name} {prop.Name} from field {fieldInfo.FieldName} [{ivalue}] ({fieldInfo.Converter?.Name})", ex);
                }
            }
        }
    }

    /// <summary>
    /// Changes the WorkItemType.
    /// </summary>
    /// <param name="newWitType">New type</param>
    public void ChangeType(string newWitType) => this.WorkItemType = newWitType;

    /// <summary>
    /// Adds a comment to the work item.
    /// </summary>
    /// <param name="text">Text to add</param>
    public void AddCommentToHistory(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(text));
        (addComments ??= new StringBuilder()).AppendLine(text);
    }

    /// <summary>
    /// Provides a text representation of the object.
    /// </summary>
    /// <returns>String</returns>
    public override string ToString()
    {
        string text = WorkItemType;
        text += Id != null ? $" {Id} ({State})" : " new";
        text += $" \"{Title}\"";
        return text;
    }

    /// <summary>
    /// Equality check
    /// </summary>
    /// <param name="other">Other object</param>
    /// <returns>True if they are equal</returns>
    public bool Equals(WorkItem? other)
    {
        return other is not null 
               && (ReferenceEquals(this, other) || 
                   (this.witModel != null && this.witModel == other.witModel)
                   || (!string.IsNullOrEmpty(this.WorkItemType) && this.Id != null)
                       && other.WorkItemType == this.WorkItemType 
                       && other.Id == this.Id);
    }

    /// <summary>
    /// Overridden object equals check
    /// </summary>
    /// <param name="obj">Other object</param>
    /// <returns>True if they are equal</returns>
    public override bool Equals(object? obj) 
        => ReferenceEquals(this, obj) || obj is WorkItem other && Equals(other);

    /// <summary>
    /// Hashcode generator for WorkItem.
    /// </summary>
    /// <returns>Unique hashcode</returns>
    public override int GetHashCode() 
        => HashCode.Combine(WorkItemType, Id??0);

    /// <summary>
    /// Equality operator
    /// </summary>
    /// <param name="left">Left side</param>
    /// <param name="right">Right side</param>
    /// <returns>True/False based on equality</returns>
    public static bool operator ==(WorkItem? left, WorkItem? right) 
        => Equals(left, right);

    /// <summary>
    /// Not equal operator
    /// </summary>
    /// <param name="left">Left side</param>
    /// <param name="right">Right side</param>
    /// <returns>True/False based on equality</returns>
    public static bool operator !=(WorkItem? left, WorkItem? right) 
        => !Equals(left, right);

    /// <summary>
    /// Return the work item type if known.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    protected static string GetWorkItemType(Type type)
    {
        return type.GetCustomAttribute<AzDOWorkItemAttribute>()?.WorkItemType ?? string.Empty;
    }
}