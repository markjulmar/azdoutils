namespace Julmar.AzDOUtilities.Agile;

/// <summary>
/// Tracks an activity the user will be able to perform with the product
/// </summary>
[AzDOWorkItem("User Story")]
public class UserStoryWorkItem : WorkItem
{
    /// <summary>
    /// Story points
    /// </summary>
    [AzDOField(WorkItemField.StoryPoints)]
    public decimal? StoryPoints { get; set; }

    /// <summary>
    /// The acceptance criteria
    /// </summary>
    [AzDOField(WorkItemField.AcceptanceCriteria)]
    public string? AcceptanceCriteria { get; set; }

    /// <summary>
    /// Uncertainty in requirement or design
    /// </summary>
    [AzDOField(WorkItemField.Risk)]
    public string? Risk { get; set; }

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
    /// Business = Customer-facing feature; Architectural = Technology initiatives to support current and future business needs
    /// </summary>
    [AzDOField(WorkItemField.ValueArea)]
    public string? ValueArea { get; set; }

    /// <summary>
    /// Product build number the task was completed in.
    /// </summary>
    [AzDOField(WorkItemField.IntegrationBuild)]
    public string? IntegrationBuild { get; set; }
}