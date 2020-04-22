using System.Collections.Generic;

namespace Julmar.AzDOUtilities
{
    public enum Relationship
    {
        Dependency,
        Related,
        Child,
        Parent,
        Affects,
        AffectedBy,
        Duplicate,
        DuplicateOf,
        ReferencedBy,
        TestedBy,
        Tests,
        TestCaseSharedSteps,
        ProducedFor,
        ConsumedBy,
        RemoteRelated,
        Hyperlink,
        ArtifactLink,
        Other,
        All,
    }

    public sealed class RelationLinks
    {
        public string Title { get; internal set; }
        public string RawRelationshipType { get; internal set; }
        public Relationship Type { get; internal set; }
        public int? RelatedId { get; internal set; }
        public string Url { get; internal set; }
        public IDictionary<string,object> Attributes { get; internal set; }

        public override string ToString()
        {
            string type = (Type == Relationship.Other) ? RawRelationshipType : Type.ToString();
            return (RelatedId == null)
                ? $"{type}: {Url}"
                : $"{type} Id:{RelatedId}";
        }
    }
}
