using NLightTemplate.Tests.Generators;
using Xunit;

namespace NLightTemplate.Tests
{
    public class UnitTests
    {
        [Theory]
        [ClassData(typeof(DefaultCustomerGenerator))]
        public void EnsureDefaultConfigurationRenders(object input, string template, string expected)
        {
            Assert.Equal(expected, StringTemplate.Render(template, input));
        }

        [Theory]
        [ClassData(typeof(ConfiguredCustomerGenerator))]
        public void EnsureFluentConfigurationRenders(object input, StringTemplateConfiguration cfg, string template, string expected)
        {
            Assert.Equal(expected, StringTemplate.Render(template, input, cfg));
        }

        [Theory]
        [ClassData(typeof(FormatObjectsGenerator))]
        public void EnsureFormatAndPaddingRenders(object input, string template, string expected)
        {
            Assert.Equal(expected, StringTemplate.Render(template, input));
        }

        [Theory]
        [ClassData(typeof(Generators.IfTestGenerator))]
        public void EnsureIfTestRenders(object input, string template, string expected)
        {
            Assert.Equal(expected, StringTemplate.Render(template, input));
        }
    }
}
