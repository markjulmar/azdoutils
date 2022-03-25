using Julmar.AzDOUtilities;
using Xunit;

namespace AzDOTests
{
    public class ComparisonTests
    {
        [Fact]
        public void CommaDelimitedConverterComparesAgainstArray()
        {
            var current = new[] {"1", "2", "3"};
            var converter = new CommaSeparatedConverter();
            Assert.True(converter.Compare("1,2,3",current));
            Assert.True(converter.Compare("1, 2,  3", current));
            Assert.True(converter.Compare("2, 1, 3", current));
            Assert.False(converter.Compare("1,3,4", current));
            Assert.False(converter.Compare("1,2,3,4", current));
        }
    }
}
