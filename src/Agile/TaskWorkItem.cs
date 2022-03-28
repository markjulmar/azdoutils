namespace Julmar.AzDOUtilities.Agile;

/// <summary>
/// Task work item type
/// </summary>
[AzDOWorkItem("Task")]
public class TaskWorkItem : WorkItem
{
    /// <summary>
    /// The type of activity that is required to complete a task.
    /// </summary>
    [AzDOField(WorkItemField.Activity)]
    public string? Activity { get; set; }

    /// <summary>
    /// Original estimate to fix
    /// </summary>
    [AzDOField(WorkItemField.OriginalEstimate)]
    public decimal? OriginalEstimate { get; set; }

    /// <summary>
    /// Remaining work left
    /// </summary>
    [AzDOField(WorkItemField.RemainingWork)]
    public decimal? RemainingWork { get; set; }

    /// <summary>
    /// Completed work
    /// </summary>
    [AzDOField(WorkItemField.CompletedWork)]
    public decimal? CompletedWork { get; set; }

    /// <summary>
    /// Start date for task.
    /// </summary>
    [AzDOField(WorkItemField.StartDate)]
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Finish date for task.
    /// </summary>
    [AzDOField(WorkItemField.FinishDate)]
    public DateTime? FinishDate { get; set; }

    /// <summary>
    /// Product build number the task was completed in.
    /// </summary>
    [AzDOField(WorkItemField.IntegrationBuild)]
    public string? IntegrationBuild { get; set; }
}