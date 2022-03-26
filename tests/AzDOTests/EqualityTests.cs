using Julmar.AzDOUtilities;
using Julmar.AzDOUtilities.Agile;
using Xunit;
using WorkItem = Julmar.AzDOUtilities.WorkItem;
using WitModel = Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem;

namespace AzDOTests
{
    public class EqualityTests
    {
        [Fact]
        public void CheckSameWorkItemEqual()
        {
            var wit = new WorkItem();
            Assert.True(wit.Equals(wit));
            Assert.False(wit.Equals(null));

            var wit2 = wit;
            Assert.True(wit == wit2);

            wit2 = new WorkItem();
            Assert.False(wit == wit2);
            Assert.False(wit!.Equals(wit2));
        }

        [Fact]
        public void CheckSameDerivedWorkItemEqual()
        {
            var wit = new BugWorkItem();
            Assert.True(wit.Equals(wit));
            Assert.False(wit.Equals(null));

            var wit2 = wit;
            Assert.True(wit == wit2);

            wit2 = new BugWorkItem();
            Assert.False(wit == wit2);
            Assert.False(wit!.Equals(wit2));
        }

        [Fact]
        public void CheckInflatedWorkItemEqual()
        {
            var witModel = new WitModel() {Id = 10};
            var wit = new WorkItem();
            wit.Initialize(witModel);

            var wit2 = new WorkItem();
            wit2.Initialize(witModel);
            
            Assert.Equal(wit, wit2);
        }

        [Fact]
        public void CheckInflatedWorkItemNotEqual()
        {
            var wit = new WorkItem();
            wit.Initialize(new WitModel() { Id = 10 });

            var wit2 = new WorkItem();
            wit2.Initialize(new WitModel() { Id = 20 });

            Assert.NotEqual(wit, wit2);
        }

        [Fact]
        public void CheckDerivedInflatedWorkItem()
        {
            var wit = new BugWorkItem();
            wit.Initialize(new WitModel() { Id = 10 });

            var wit2 = new BugWorkItem();
            wit2.Initialize(new WitModel() { Id = 20 });

            Assert.NotEqual(wit, wit2);

            var wit3 = new BugWorkItem();
            wit3.Initialize(new WitModel() { Id = 10,  });
            Assert.Equal(wit, wit3);
        }

    }
}
