namespace Julmar.AzDOUtilities;

/// <summary>
/// Assembly-level attribute to register new types mapped to custom work items.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class AzDORegisterAttribute : Attribute
{
    /// <summary>
    /// Types to map
    /// </summary>
    public Type[] Types { get; }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="typedClasses">Types to map</param>
    public AzDORegisterAttribute(params Type[] typedClasses)
    {
        if (typedClasses == null) throw new ArgumentNullException(nameof(typedClasses));
        Types = typedClasses.ToArray();
    }
}