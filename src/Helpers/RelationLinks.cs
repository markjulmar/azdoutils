namespace Julmar.AzDOUtilities;

/// <summary>
/// Represents a relationship between two Azure DevOps items
/// </summary>
public sealed class RelationLinks
{
    /// <summary>
    /// Title
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// Underlying relationship type
    /// </summary>
    public string RawRelationshipType { get; init; } = string.Empty;

    /// <summary>
    /// Detected relationship type
    /// </summary>
    public Relationship Type { get; init; }
    
    /// <summary>
    /// Work Item id this relates to
    /// </summary>
    public int? RelatedId { get; init; }

    /// <summary>
    /// URL for the relationship
    /// </summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>
    /// Attributes
    /// </summary>
    public IDictionary<string, object> Attributes { get; init; } = null!;

    /// <summary>
    /// Constructor
    /// </summary>
    internal RelationLinks()
    {
    }

    /// <summary>
    /// ToString override
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        string type = Type == Relationship.Other ? RawRelationshipType : Type.ToString();
        return RelatedId == null ? $"{type}: {Url}" : $"{type} Id:{RelatedId}";
    }
}