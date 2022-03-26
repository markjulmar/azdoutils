using System.Reflection;

namespace Julmar.AzDOUtilities;

/// <summary>
/// Converter to translate an enum value to and from a field string.
/// </summary>
public class StringEnumConverter : IFieldConverter
{
    /// <summary>
    /// Convert the field value to the enumeration type.
    /// </summary>
    /// <param name="value">Field value</param>
    /// <param name="toType">Enumeration type</param>
    /// <returns>Enum</returns>
    public object? Convert(object? value, Type toType)
    {
        if (toType == null) throw new ArgumentNullException(nameof(toType));

        // Enum types can be nullable.
        var nullableType = Nullable.GetUnderlyingType(toType);
        if (nullableType != null)
            toType = nullableType;

        if (!toType.IsEnum) throw new ArgumentException($"{toType.Name} is not an enum.");

        string fieldText = value?.ToString() ?? "";
        if (string.IsNullOrEmpty(fieldText)) 
            return null;

        // Try attributes first. Go through all of them.
        foreach (var enumValue in toType.GetEnumValues())
        {
            string? enumText = toType
                .GetField(enumValue.ToString()!)!
                .GetCustomAttribute<AzDOEnumValueAttribute>()?
                .Value;
            if (enumText != null)
            {
                if (string.Compare(fieldText, enumText, StringComparison.CurrentCultureIgnoreCase) == 0)
                    return enumValue;
            }
        }

        // Try direct values as a fallback.
        foreach (var enumValue in toType.GetEnumValues())
        {
            var enumText = enumValue.ToString()!;
            if (string.Compare(fieldText, enumText, StringComparison.CurrentCultureIgnoreCase) == 0)
                return enumValue;
        }

        return null;
    }

    /// <summary>
    /// Convert an enumeration type back to a field value.
    /// </summary>
    /// <param name="value">Enumeration type</param>
    /// <returns>Field value</returns>
    public object? ConvertBack(object? value)
    {
        if (value == null) return null;

        Type enumType = value.GetType();
        string? enumText = enumType
            .GetField(value.ToString()!)!
            .GetCustomAttribute<AzDOEnumValueAttribute>()?
            .Value;
        return enumText ?? value.ToString();
    }
}