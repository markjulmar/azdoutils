namespace Julmar.AzDOUtilities;

/// <summary>
/// Interface to convert a field to a specific .NET type
/// </summary>
public interface IFieldConverter
{
    /// <summary>
    /// Converts a field to a type
    /// </summary>
    /// <param name="value">Value to convert</param>
    /// <param name="toType">Type to convert to</param>
    /// <returns>New object of type with value</returns>
    object? Convert(object? value, Type toType);

    /// <summary>
    /// Converts a type back to a field
    /// </summary>
    /// <param name="value">Value to convert</param>
    /// <returns>Field value</returns>
    object? ConvertBack(object? value);
}