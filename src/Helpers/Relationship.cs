using System.Xml.Serialization;

namespace Julmar.AzDOUtilities;

/// <summary>
/// Azure DevOps relationship types
/// </summary>
public enum Relationship
{
    /// <summary>
    /// Dependency
    /// </summary>
    [XmlAttribute("System.LinkTypes.Dependency")] Dependency,

    /// <summary>
    /// Relationship
    /// </summary>
    [XmlAttribute("System.LinkTypes.Related")] Related,

    /// <summary>
    /// Child
    /// </summary>
    [XmlAttribute("System.LinkTypes.Hierarchy-Forward")] Child,

    /// <summary>
    /// Parent
    /// </summary>
    [XmlAttribute("System.LinkTypes.Hierarchy-Reverse")] Parent,

    /// <summary>
    /// Affects
    /// </summary>
    [XmlAttribute("Microsoft.VSTS.Common.Affects-Forward")] Affects,

    /// <summary>
    /// Affected by
    /// </summary>
    [XmlAttribute("Microsoft.VSTS.Common.Affects-Reverse")] AffectedBy,

    /// <summary>
    /// Duplicate
    /// </summary>
    [XmlAttribute("System.LinkTypes.Duplicate-Forward")] Duplicate,

    /// <summary>
    /// Duplicate of
    /// </summary>
    [XmlAttribute("System.LinkTypes.Duplicate-Reverse")] DuplicateOf,

    /// <summary>
    /// Referenced by
    /// </summary>
    [XmlAttribute("Microsoft.VSTS.TestCase.SharedParameterReferencedBy")] ReferencedBy,

    /// <summary>
    /// Tested by
    /// </summary>
    [XmlAttribute("Microsoft.VSTS.Common.TestedBy-Forward")] TestedBy,

    /// <summary>
    /// Tests x
    /// </summary>
    [XmlAttribute("Microsoft.VSTS.Common.TestedBy-Reverse")] Tests,

    /// <summary>
    /// Shares test steps
    /// </summary>
    [XmlAttribute("Microsoft.VSTS.TestCase.SharedStepReferencedBy")] TestCaseSharedSteps,

    /// <summary>
    /// Produced for
    /// </summary>
    [XmlAttribute("Microsoft.VSTS.Common.ProducedFor.Forward")] ProducedFor,

    /// <summary>
    /// Consumed by
    /// </summary>
    [XmlAttribute("Microsoft.VSTS.Common.ConsumesFrom.Reverse")] ConsumedBy,

    /// <summary>
    /// Remove relationship to a different AzDO instance
    /// </summary>
    [XmlAttribute("System.LinkTypes.Remote.Related")] RemoteRelated,

    /// <summary>
    /// Hyperlink
    /// </summary>
    [XmlAttribute("Hyperlink")] Hyperlink,

    /// <summary>
    /// Artifact of
    /// </summary>
    [XmlAttribute("ArtifactLink")] ArtifactLink,

    /// <summary>
    /// Other link
    /// </summary>
    Other,

    /// <summary>
    /// All
    /// </summary>
    All
}