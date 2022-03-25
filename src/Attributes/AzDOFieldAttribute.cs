namespace Julmar.AzDOUtilities;

/// <summary>
/// Attribute that ties a property to a specific field in an Azure DevOps work item.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class AzDOFieldAttribute : Attribute
{
    /// <summary>
    /// Field name to tie to the property.
    /// </summary>
    public string FieldName { get; }

    /// <summary>
    /// Optional converter to use when transferring a value to/from the property.
    /// </summary>
    public Type? Converter { get; init; }

    /// <summary>
    /// True if this is a one-way transfer
    /// </summary>
    public bool IsReadOnly { get; init; }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="fieldName">Azure DevOps work item field name</param>
    /// <exception cref="ArgumentNullException"></exception>
    public AzDOFieldAttribute(string fieldName)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
            throw new ArgumentNullException(nameof(fieldName), "Missing field name.");

        FieldName = fieldName;
    }
}