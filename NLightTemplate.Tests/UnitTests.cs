using Newtonsoft.Json;
using NLightTemplate.Tests.Generators;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using Xunit;

namespace NLightTemplate.Tests
{
    public class UnitTests
    {
        [Theory]
        [ClassData(typeof(DefaultCustomerGenerator))]
        public void EnsureDefaultConfigurationRenders(object input, string template, string expected, bool isDynamic)
        {
            var props = StringTemplate.BuildPropertyDictionary(input);
            Assert.Equal(expected, StringTemplate.Render(template, input));
            if (isDynamic)
            {
                var dyn = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(input));
                var rendered = StringTemplate.Render(template, dyn);
                Assert.Equal(expected, rendered);
            }
        }

        [Theory]
        [ClassData(typeof(ConfiguredCustomerGenerator))]
        public void EnsureFluentConfigurationRenders(object input, StringTemplateConfiguration cfg, string template, string expected)
        {
            Assert.Equal(expected, StringTemplate.Render(template, input, cfg));
            dynamic dyn = input.ToDynamic();
            Assert.Equal(expected, StringTemplate.Render(template, dyn, cfg));
        }

        [Theory]
        [ClassData(typeof(FormatObjectsGenerator))]
        public void EnsureFormatAndPaddingRenders(object input, string template, string expected)
        {
            Assert.Equal(expected, StringTemplate.Render(template, input));
            dynamic dyn = input.ToDynamic();
            Assert.Equal(expected, StringTemplate.Render(template, dyn));
        }

        [Theory]
        [ClassData(typeof(Generators.IfTestGenerator))]
        public void EnsureIfTestRenders(object input, string template, string expected)
        {
            Assert.Equal(expected, StringTemplate.Render(template, input));
            dynamic dyn = input.ToDynamic();
            Assert.Equal(expected, StringTemplate.Render(template, dyn));
        }
    }

    public static class Extensions
    {
        public static dynamic ToDynamic(this object value)
        {
            IDictionary<string, object> expando = new ExpandoObject();

            foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(value.GetType()))
                expando.Add(property.Name, property.GetValue(value));

            return expando as ExpandoObject;
        }
    }
}
