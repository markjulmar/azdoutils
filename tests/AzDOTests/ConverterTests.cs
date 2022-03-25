using System;
using System.Collections.Generic;
using System.Linq;
using Julmar.AzDOUtilities;
using Xunit;

namespace AzDOTests
{
    public class ConverterTests
    {
        [Fact]
        public void CommaSeparatedConverterConvertsToList()
        {
            string input = "One,Two,Three";
            var expected = input.Split(',').ToList();

            var converter = new CommaSeparatedConverter();
            var output = converter.Convert(input, typeof(List<string>));

            Assert.Equal(expected, output);
        }

        [Fact]
        public void CommaSeparatedConverterConvertsArray()
        {
            string input = "One,Two,Three";
            var expected = input.Split(',').ToArray();

            var converter = new CommaSeparatedConverter();
            var output = converter.Convert(input, typeof(string[]));

            Assert.Equal(expected, output);
        }

        [Fact]
        public void CommaSeparatedConverterThrowsWhenNotCollection()
        {
            string input = "One,Two,Three";

            var converter = new CommaSeparatedConverter();
            Assert.Throws<ArgumentException>(() => converter.Convert(input, typeof(string)));
        }

        [Fact]
        public void ConverterReturnsEmptyArrayWhenGivenNull()
        {
            var converter = new CommaSeparatedConverter();
            Assert.Empty((string[])converter.Convert(null, typeof(string[])));
            Assert.Empty((List<string>)converter.Convert(null, typeof(List<string>)));
        }

        [Fact]
        public void ConverterReturnsEmptyStringWhenGivenNull()
        {
            var converter = new CommaSeparatedConverter();
            Assert.Equal("", converter.ConvertBack(null));
            Assert.Equal("", converter.ConvertBack(Array.Empty<string>()));
            Assert.Equal("", converter.ConvertBack(Enumerable.Empty<string>()));
            Assert.Throws<ArgumentException>(() => converter.ConvertBack(""));
        }

        [Fact]
        public void CommaSeparatedConverterConvertsBackArray()
        {
            string expected = "One, Two, Three";
            var input = new[] {"One", "Two", "Three"};

            var converter = new CommaSeparatedConverter();
            var output = converter.ConvertBack(input);

            Assert.Equal(expected, output);
        }

        [Fact]
        public void CommaSeparatedConverterPreservesSpaces()
        {
            string expected = "One , Two, Three";
            var input = new[] { "One ", "Two", "Three" };

            var converter = new CommaSeparatedConverter();
            var output = converter.ConvertBack(input);

            Assert.Equal(expected, output);
        }

    }
}