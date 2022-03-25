namespace Julmar.AzDOUtilities;

/// <summary>
/// Converter to split out a semicolon separated list of values
/// </summary>
public sealed class SemicolonSeparatedConverter : SeparatedValueConverter
{
    /// <summary>
    /// Constructor
    /// </summary>
    public SemicolonSeparatedConverter() : base("; ") {}
}