namespace Julmar.AzDOUtilities.Agile;

/// <summary>
/// Bug work item. Describes a divergence between required and actual behavior, and tracks the work done to
/// correct the defect and verify the correction.
/// </summary>
[AzDOWorkItem("Bug")]
public class BugWorkItem : WorkItem
{
    /// <summary>
    /// Reproduction steps
    /// </summary>
    [AzDOField(WorkItemField.ReproSteps)]
    public string? ReproSteps { get; set; }

    /// <summary>
    /// System information
    /// </summary>
    [AzDOField(WorkItemField.SystemInfo)]
    public string? SystemInfo { get; set; }

    /// <summary>
    /// Story points
    /// </summary>
    [AzDOField(WorkItemField.StoryPoints)]
    public decimal? StoryPoints { get; set; }

    /// <summary>
    /// The type of activity that is required to complete a task.
    /// </summary>
    [AzDOField(WorkItemField.Activity)]
    public string? Activity { get; set; }

    /// <summary>
    /// Severity
    /// </summary>
    [AzDOField(WorkItemField.Severity, Converter = typeof(StringEnumConverter))]
    public BugSeverity? Severity { get; set; }

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
    /// Product build number the bug was found in.
    /// </summary>
    [AzDOField(WorkItemField.FoundIn)]
    public string? FoundInBuild { get; set; }

    /// <summary>
    /// Product build number the bug was fixed in.
    /// </summary>
    [AzDOField(WorkItemField.IntegrationBuild)]
    public string? FixedInBuild { get; set; }

    /// <summary>
    /// Business primarily to represent customer-facing issues.
    /// </summary>
    [AzDOField(WorkItemField.ValueArea)]
    public string? ValueArea { get; set; }
}