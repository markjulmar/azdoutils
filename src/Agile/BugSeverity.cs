namespace Julmar.AzDOUtilities.Agile;

/// <summary>
/// Bug severity
/// </summary>
public enum BugSeverity
{
    /// <summary>
    /// Critical issue
    /// </summary>
    [AzDOEnumValue("1 - Critical")]
    Critical = 1,

    /// <summary>
    /// High
    /// </summary>
    [AzDOEnumValue("2 - High")]
    High = 2,

    /// <summary>
    /// Medium
    /// </summary>
    [AzDOEnumValue("3 - Medium")]
    Medium = 3,

    /// <summary>
    /// Low
    /// </summary>
    [AzDOEnumValue("4 - Low")]
    Low = 4
}