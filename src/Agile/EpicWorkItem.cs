namespace Julmar.AzDOUtilities.Agile;

/// <summary>
/// Epic work item type
/// </summary>
[AzDOWorkItem("Epic")]
public class EpicWorkItem : WorkItem
{
    /// <summary>
    /// Uncertainty in epic.
    /// </summary>
    [AzDOField(WorkItemField.Risk)]
    public string? Risk { get; set; }

    /// <summary>
    /// The business value for the customer when this epic is released.
    /// </summary>
    [AzDOField(WorkItemField.BusinessValue)]
    public int? BusinessValue { get; set; }

    /// <summary>
    /// The estimated effort to implemented the epic
    /// </summary>
    [AzDOField(WorkItemField.Effort)]
    public double? Effort { get; set; }

    /// <summary>
    /// Start date for epic.
    /// </summary>
    [AzDOField(WorkItemField.StartDate)]
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Target date for epic.
    /// </summary>
    [AzDOField(WorkItemField.TargetDate)]
    public DateTime? TargetDate { get; set; }

    /// <summary>
    /// How does the business value decay over time. Higher values make the epic more time critical
    /// </summary>
    [AzDOField(WorkItemField.TimeCriticality)]
    public double? TimeCriticality { get; set; }

    /// <summary>
    /// Product build number the epic was implemented in.
    /// </summary>
    [AzDOField(WorkItemField.IntegrationBuild)]
    public string? IntegrationBuild { get; set; }

    /// <summary>
    /// Business = Customer-facing epics; Architectural = Technology initiatives to support current and future business needs
    /// </summary>
    [AzDOField(WorkItemField.ValueArea)]
    public string? ValueArea { get; set; }
}