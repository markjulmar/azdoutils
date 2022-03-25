using Microsoft.VisualStudio.Services.WebApi;

namespace Julmar.AzDOUtilities;

/// <summary>
/// Converts an Azure DevOps identity (person) to a string.
/// </summary>
public class IdentityRefConverter : IFieldConverter, IFieldComparer
{
    /// <summary>
    /// Compare an IdentityRef with a string value
    /// </summary>
    /// <param name="initialValue">Initial field value</param>
    /// <param name="currentValue"></param>
    /// <returns></returns>
    public bool Compare(object? initialValue, object? currentValue)
    {
        if (initialValue == null &&
            string.IsNullOrEmpty(currentValue?.ToString()))
            return true;

        return initialValue is IdentityRef identity 
               && string.Compare(currentValue?.ToString()??"", 
                   Convert(identity), StringComparison.OrdinalIgnoreCase) == 0;
    }

    /// <summary>
    /// Convert an IdentityRef to a string
    /// </summary>
    /// <param name="identity"></param>
    /// <returns></returns>
    private static string Convert(IdentityRef identity) => identity.DisplayName + $" <{identity.UniqueName}>";

    /// <summary>
    /// IFieldConverter implementation to convert an IdentityRef to a string
    /// </summary>
    /// <param name="value">IdentityRef</param>
    /// <param name="toType">typeof(string)</param>
    /// <returns>string representation of identity</returns>
    /// <exception cref="Exception"></exception>
    public object? Convert(object? value, Type toType)
    {
        if (toType != typeof(string))
            throw new Exception(nameof(IdentityRefConverter) + " can only convert to " + nameof(String));
        
        return value switch
        {
            null => null,
            IdentityRef identity => Convert(identity),
            string s => s,
            _ => string.Empty
        };
    }

    /// <summary>
    /// Convert a string back to an IdentityRef
    /// </summary>
    /// <param name="value">string</param>
    /// <returns></returns>
    public object? ConvertBack(object? value) => value;
}