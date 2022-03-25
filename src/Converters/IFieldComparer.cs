namespace Julmar.AzDOUtilities;

/// <summary>
/// Interface used to compare a field value
/// </summary>
public interface IFieldComparer
{
    /// <summary>
    /// Compare a filed vs. property value.
    /// </summary>
    /// <param name="initialValue">Starting value</param>
    /// <param name="currentValue">Current value</param>
    /// <returns>True if the two values are equivalent</returns>
    bool Compare(object? initialValue, object? currentValue);
}