using AzDOTests;
using Julmar.AzDOUtilities;
using Xunit;

[assembly: AzDORegister(typeof(TestFeatureWorkItem))]

namespace AzDOTests
{
    public class TestFeatureWorkItem : Julmar.AzDOUtilities.Agile.FeatureWorkItem
    {
    }

    public class RegistrationTests
    {
        [Fact]
        public void StandardTypesAreRegistered()
        {
            Assert.True(ReflectionHelpers.RegisteredTypes.ContainsKey("Bug"));
            Assert.True(ReflectionHelpers.RegisteredTypes["Bug"] == typeof(Julmar.AzDOUtilities.Agile.BugWorkItem));
        }

        [Fact]
        public void StandardTypesCanBeOverridden()
        {
            var types = ReflectionHelpers.RegisteredTypes;
            Assert.True(types.ContainsKey("Feature"));
            Assert.True(types["Feature"] == typeof(TestFeatureWorkItem));
        }
    }
}
