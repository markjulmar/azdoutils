namespace Julmar.AzDOUtilities;

/// <summary>
/// Available logging levels
/// </summary>
[Flags]
public enum LogLevel
{
    /// <summary>
    /// No logging
    /// </summary>
    None = 0,

    /// <summary>
    /// Trace queries
    /// </summary>
    Query = 1,

    /// <summary>
    /// Trace LINQ expressions
    /// </summary>
    LinqQuery = 2,

    /// <summary>
    /// Trace all method entry/exit events
    /// </summary>
    EnterExit = 4,

    /// <summary>
    /// Dump all patch documents sent to Azure DevOps
    /// </summary>
    PatchDocument = 8,

    /// <summary>
    /// Trace related API calls
    /// </summary>
    RelatedApis = 16,

    /// <summary>
    /// Trace LINQ expressions
    /// </summary>
    LinqExpression = 32,

    /// <summary>
    /// Trace raw API calls made
    /// </summary>
    RawApis = 64
}