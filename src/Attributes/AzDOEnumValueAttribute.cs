namespace Julmar.AzDOUtilities;

/// <summary>
/// This is applied to enumerations to provide a specific value to match when
/// transferring values to/from a string field type.
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class AzDOEnumValueAttribute : Attribute
{
    /// <summary>
    /// The expected value for this enumeration type.
    /// </summary>
    public string Value { get; set; }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="value">Value for this enumeration</param>
    public AzDOEnumValueAttribute(string value)
    {
        Value = value;
    }
}