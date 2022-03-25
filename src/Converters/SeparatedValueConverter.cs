namespace Julmar.AzDOUtilities;

/// <summary>
/// Field converter to manage a list of values with a known separator character
/// </summary>
public class SeparatedValueConverter : IFieldConverter, IFieldComparer
{
    private readonly string separator;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="separator">Separator for the values</param>
    protected SeparatedValueConverter(string separator)
    {
        this.separator = separator ?? throw new ArgumentNullException(nameof(separator));
    }

    /// <summary>
    /// Compare a filed vs. property value.
    /// </summary>
    /// <param name="initialValue">Starting value (Separated list)</param>
    /// <param name="currentValue">Current value (Enumerable)</param>
    /// <returns>True if the two values are equivalent</returns>
    public bool Compare(object? initialValue, object? currentValue)
    {
        if (initialValue == null && currentValue == null) return true;
        if (initialValue == null && currentValue != null
            || initialValue != null && currentValue == null) return false;

        if (Convert(initialValue, typeof(List<string>)) is not List<string> initial) 
            return false;

        var current = ((IEnumerable<string>?)currentValue)?.ToList() ?? new List<string>();
        if (initial.Count == current.Count)
        {
            if (initial.Count == 0)
                return true;

            initial.Sort(); current.Sort(); // ensure same order.
            return initial.SequenceEqual(current);
        }

        return false;
    }

    /// <summary>
    /// Converts a field to a type
    /// </summary>
    /// <param name="value">Value to convert</param>
    /// <param name="toType">Type to convert to</param>
    /// <returns>New object of type with value</returns>
    public virtual object Convert(object? value, Type toType)
    {
        if (toType != typeof(string[]) && toType != typeof(List<string>))
            throw new ArgumentException($"Cannot convert {value?.GetType().Name ?? "<unknown>"} to {toType.Name}", nameof(value));

        string text = value?.ToString()??"";
        string[] values = text.Split(separator.Trim(), StringSplitOptions.RemoveEmptyEntries);
        for (int n = 0; n < values.Length; n++)
            values[n] = values[n].Trim();
        return toType == typeof(List<string>) ? values.ToList() : values;
    }

    /// <summary>
    /// Converts a type back to a field
    /// </summary>
    /// <param name="value">Value to convert</param>
    /// <returns>Field value</returns>
    public virtual object ConvertBack(object? value)
    {
        switch (value)
        {
            case null:
                return string.Empty;
            case IEnumerable<string> enumerable:
            {
                var result = string.Join(separator, enumerable);
                return string.IsNullOrWhiteSpace(result) ? string.Empty : result;
            }
            default:
                throw new ArgumentException($"Cannot convert {value} to string", nameof(value));
        }
    }
}