namespace Julmar.AzDOUtilities;

/// <summary>
/// Converter to split out a comma-separated list of values.
/// </summary>
public sealed class CommaSeparatedConverter : SeparatedValueConverter
{
    /// <summary>
    /// Constructor
    /// </summary>
    public CommaSeparatedConverter() : base(", ") {}
}