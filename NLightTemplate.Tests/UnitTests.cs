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
        [ClassData(typeof(IfTestGenerator))]
        public void EnsureIfTestRenders(object input, string template, string expected)
        {
            Assert.Equal(expected, StringTemplate.Render(template, input));
            dynamic dyn = input.ToDynamic();
            Assert.Equal(expected, StringTemplate.Render(template, dyn));
        }

        [Theory]
        [ClassData(typeof(CustomEnumerableGenerator))]
        public void EnsureCustomEnumerableRenders(object input, string template, string expected)
        {
            Assert.Equal(expected, StringTemplate.Render(template, input));
        }

        [Fact]
        public void InstanceRendererUsesDefaultConfiguration()
        {
            var renderer = new TemplateRenderer();
            Assert.Equal("Hello John Doe!", renderer.Render("Hello {FullName}!", Customer.GenerateDemo()));
        }

        [Fact]
        public void InstanceRendererHonorsSuppliedConfiguration()
        {
            var cfg = new FluentStringTemplateConfiguration().OpenToken("<$").CloseToken("$>").ExposeConfiguration();
            var renderer = new TemplateRenderer(cfg);
            Assert.Equal("Hello John Doe!", renderer.Render("Hello <$FullName$>!", Customer.GenerateDemo()));
        }

        [Fact]
        public void InstanceRendererHonorsConfigurationFactory()
        {
            IStringTemplateConfiguration cfg = StringTemplateConfiguration.Create(c => c.OpenToken("<%").CloseToken("%>"));
            var renderer = new TemplateRenderer(cfg);
            Assert.Equal("Hello John Doe!", renderer.Render("Hello <%FullName%>!", Customer.GenerateDemo()));
        }

        [Fact]
        public void InstanceRendererHonorsCustomInterfaceImplementation()
        {
            // A non-StringTemplateConfiguration IStringTemplateConfiguration is snapshotted into a concrete config.
            var renderer = new TemplateRenderer(new CustomConfiguration());
            Assert.Equal("Hello John Doe!", renderer.Render("Hello <<FullName>>!", Customer.GenerateDemo()));
        }
    }

    /// <summary>A bespoke <see cref="IStringTemplateConfiguration"/> that isn't a <see cref="StringTemplateConfiguration"/>.</summary>
    public class CustomConfiguration : IStringTemplateConfiguration
    {
        public string OpenToken => "<<";
        public string CloseToken => ">>";
        public string ForeachToken => "foreach";
        public string IfToken => "if";
        public string ElseToken => "else";
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
