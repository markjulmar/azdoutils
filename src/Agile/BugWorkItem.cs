namespace Julmar.AzDOUtilities.Agile;

/// <summary>
/// Bug work item. Describes a divergence between required and actual behavior, and tracks the work done to
/// correct the defect and verify the correction.
/// </summary>
[AzDOWorkItem("Bug")]
public class BugWorkItem : WorkItem
{
    /// <summary>
    /// The work item type string for this work item
    /// </summary>
    public static string Type => WorkItem.GetWorkItemType(typeof(EpicWorkItem));

    /// <summary>
    /// Reproduction steps
    /// </summary>
    [AzDOField(Field.ReproSteps)]
    public string? ReproSteps { get; set; }

    /// <summary>
    /// System information
    /// </summary>
    [AzDOField(Field.SystemInfo)]
    public string? SystemInfo { get; set; }

    /// <summary>
    /// Story points
    /// </summary>
    [AzDOField(Field.StoryPoints)]
    public decimal? StoryPoints { get; set; }

    /// <summary>
    /// The type of activity that is required to complete a task.
    /// </summary>
    [AzDOField(Field.Activity)]
    public string? Activity { get; set; }

    /// <summary>
    /// Severity
    /// </summary>
    [AzDOField(Field.Severity, Converter = typeof(StringEnumConverter))]
    public BugSeverity? Severity { get; set; }

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
    /// Product build number the bug was found in.
    /// </summary>
    [AzDOField(Field.FoundIn)]
    public string? FoundInBuild { get; set; }

    /// <summary>
    /// Product build number the bug was fixed in.
    /// </summary>
    [AzDOField(Field.IntegrationBuild)]
    public string? FixedInBuild { get; set; }

    /// <summary>
    /// Business primarily to represent customer-facing issues.
    /// </summary>
    [AzDOField(Field.ValueArea)]
    public string? ValueArea { get; set; }
}