namespace Julmar.AzDOUtilities;

/// <summary>
/// Attribute applied to .NET types to tie it to a specific custom Azure DevOps work item type
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class AzDOWorkItemAttribute : Attribute
{
    /// <summary>
    /// Work item type name
    /// </summary>
    public string WorkItemType { get; }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="workItemType">Work item type name</param>
    public AzDOWorkItemAttribute(string workItemType)
    {
        WorkItemType = workItemType ?? throw new ArgumentNullException(nameof(workItemType));
    }
}