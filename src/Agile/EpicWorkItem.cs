namespace Julmar.AzDOUtilities.Agile;

/// <summary>
/// Epic work item type
/// </summary>
[AzDOWorkItem("Epic")]
public class EpicWorkItem : WorkItem
{
    /// <summary>
    /// The work item type string for this work item
    /// </summary>
    public static string Type => WorkItem.GetWorkItemType(typeof(EpicWorkItem));

    /// <summary>
    /// Uncertainty in epic.
    /// </summary>
    [AzDOField(Field.Risk)]
    public string? Risk { get; set; }

    /// <summary>
    /// The business value for the customer when this epic is released.
    /// </summary>
    [AzDOField(Field.BusinessValue)]
    public int? BusinessValue { get; set; }

    /// <summary>
    /// The estimated effort to implemented the epic
    /// </summary>
    [AzDOField(Field.Effort)]
    public double? Effort { get; set; }

    /// <summary>
    /// Start date for epic.
    /// </summary>
    [AzDOField(Field.StartDate)]
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Target date for epic.
    /// </summary>
    [AzDOField(Field.TargetDate)]
    public DateTime? TargetDate { get; set; }

    /// <summary>
    /// How does the business value decay over time. Higher values make the epic more time critical
    /// </summary>
    [AzDOField(Field.TimeCriticality)]
    public double? TimeCriticality { get; set; }

    /// <summary>
    /// Product build number the epic was implemented in.
    /// </summary>
    [AzDOField(Field.IntegrationBuild)]
    public string? IntegrationBuild { get; set; }

    /// <summary>
    /// Business = Customer-facing epics; Architectural = Technology initiatives to support current and future business needs
    /// </summary>
    [AzDOField(Field.ValueArea)]
    public string? ValueArea { get; set; }
}