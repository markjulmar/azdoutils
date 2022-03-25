using System;
using Julmar.AzDOUtilities;
using Xunit;

namespace AzDOTests
{
    public class ReflectionTests
    {
        [Fact]
        public void TestRelationshipLinkTextConversion()
        {
            Assert.Equal("System.LinkTypes.Hierarchy-Forward", 
                AzDOService.GetRelationshipLinkText(Relationship.Child));
            Assert.Throws<ArgumentOutOfRangeException>(() 
                => AzDOService.GetRelationshipLinkText(Relationship.Other));

            Assert.Equal(Relationship.AffectedBy, 
                AzDOService.GetRelationshipFromLinkText("Microsoft.VSTS.Common.Affects-Reverse"));
            Assert.Equal(Relationship.Other,
                AzDOService.GetRelationshipFromLinkText(""));
            Assert.Equal(Relationship.Other,
                AzDOService.GetRelationshipFromLinkText(null));
        }

        [Fact]
        public void CanParseRelIdFromUrl()
        {
            string value = "this-is-a-url/relationship/1";
            Assert.Equal(1, AzDOService.ParseIdFromRelationship(value));
            Assert.Null(AzDOService.ParseIdFromRelationship("relationship/rel"));
            Assert.Null(AzDOService.ParseIdFromRelationship("relationship"));
            Assert.Null(AzDOService.ParseIdFromRelationship(""));
            Assert.Null(AzDOService.ParseIdFromRelationship(null));
            Assert.Null(AzDOService.ParseIdFromRelationship("1/relationship/"));
        }

    }
}
