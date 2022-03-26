namespace Julmar.AzDOUtilities.Agile;

/// <summary>
/// Task work item type
/// </summary>
[AzDOWorkItem("Task")]
public class TaskWorkItem
{
    /// <summary>
    /// The type of activity that is required to complete a task.
    /// </summary>
    [AzDOField(Field.Activity)]
    public string? Activity { get; set; }

    /// <summary>
    /// Original estimate to fix
    /// </summary>
    [AzDOField(Field.OriginalEstimate)]
    public decimal? OriginalEstimate { get; set; }

    /// <summary>
    /// Remaining work left
    /// </summary>
    [AzDOField(Field.RemainingWork)]
    public decimal? RemainingWork { get; set; }

    /// <summary>
    /// Completed work
    /// </summary>
    [AzDOField(Field.CompletedWork)]
    public decimal? CompletedWork { get; set; }

    /// <summary>
    /// Start date for task.
    /// </summary>
    [AzDOField(Field.StartDate)]
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Finish date for task.
    /// </summary>
    [AzDOField(Field.FinishDate)]
    public DateTime? FinishDate { get; set; }

    /// <summary>
    /// Product build number the task was completed in.
    /// </summary>
    [AzDOField(Field.IntegrationBuild)]
    public string? IntegrationBuild { get; set; }
}