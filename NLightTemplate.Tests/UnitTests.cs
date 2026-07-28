using Newtonsoft.Json;
using NLightTemplate.Tests.Generators;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Globalization;
using System.Runtime.CompilerServices;
using Xunit;

namespace NLightTemplate.Tests
{
    public class UnitTests
    {
        // The advanced-template fixtures hardcode CRLF line endings and en-US date/number formatting.
        // Normalize both sides so comparisons are robust to the checkout line endings and to ICU's
        // narrow (U+202F) / no-break (U+00A0) space before AM/PM, so the tests pass on Windows, Linux, and macOS.
        private static string Normalize(string value) =>
            value?.Replace("\r\n", "\n").Replace(" ", " ").Replace(" ", " ");

        [Theory]
        [ClassData(typeof(DefaultCustomerGenerator))]
        public void EnsureDefaultConfigurationRenders(object input, string template, string expected, bool isDynamic)
        {
            Assert.Equal(Normalize(expected), Normalize(StringTemplate.Render(template, input)));
            if (isDynamic)
            {
                var dyn = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(input));
                var rendered = StringTemplate.Render(template, dyn);
                Assert.Equal(Normalize(expected), Normalize(rendered));
            }
        }

        [Theory]
        [ClassData(typeof(ConfiguredCustomerGenerator))]
        public void EnsureFluentConfigurationRenders(object input, StringTemplateConfiguration cfg, string template, string expected)
        {
            Assert.Equal(Normalize(expected), Normalize(StringTemplate.Render(template, input, cfg)));
            dynamic dyn = input.ToDynamic();
            Assert.Equal(Normalize(expected), Normalize(StringTemplate.Render(template, dyn, cfg)));
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

        [Fact]
        public void FormatSpecifiersWorkWithRegexMetacharacterTokens()
        {
            // '[' and ']' are regex metacharacters; the format/padding matcher must escape the tokens.
            var cfg = new FluentStringTemplateConfiguration().OpenToken("[[").CloseToken("]]").ExposeConfiguration();
            Assert.Equal("Total: 12.50", StringTemplate.Render("Total: [[Amount:0.00]]", new { Amount = 12.5 }, cfg));
        }

        [Fact]
        public void FormatSpecifiersEscapeDottedKeys()
        {
            // The '.' in a dotted key must be matched literally, not as the regex "any character".
            Assert.Equal("9.50", StringTemplate.Render("{Product.Price:0.00}", new { Product = new { Price = 9.5 } }));
        }

        [Fact]
        public void InheritedPropertiesAreRendered()
        {
            // Id and Kind are declared on the base class; they must still be reflected and rendered.
            var model = new DerivedEntity { Id = 7, Name = "Widget" };
            Assert.Equal("7 Widget base", StringTemplate.Render("{Id} {Name} {Kind}", model));
        }

        [Fact]
        public void MostDerivedPropertyWinsWhenHidden()
        {
            // A 'new'-hidden property must not cause a duplicate-key failure; the derived declaration wins.
            var model = new ShadowDerived();
            Assert.Equal("derived", StringTemplate.Render("{Label}", model));
        }

        [Fact]
        public void ForeachExposesLoopMetadata()
        {
            var data = new { Items = new[] { new { Name = "A" }, new { Name = "B" }, new { Name = "C" } } };
            Assert.Equal("[0/3 True False A][1/3 False False B][2/3 False True C]",
                StringTemplate.Render("{foreach Items}[{index}/{count} {first} {last} {Name}]{/foreach Items}", data));
        }

        [Fact]
        public void LoopMetadataComposesWithIfForSeparators()
        {
            var data = new { Items = new[] { new { Name = "A" }, new { Name = "B" }, new { Name = "C" } } };
            Assert.Equal("A, B, C",
                StringTemplate.Render("{foreach Items}{Name}{if last}{else}, {/if last}{/foreach Items}", data));
        }

        [Fact]
        public void NestedForeachHaveIndependentLoopMetadata()
        {
            var data = new
            {
                Outer = new[]
                {
                    new { Inner = new[] { new { }, new { } } },
                    new { Inner = new[] { new { }, new { } } }
                }
            };
            Assert.Equal("0:01;1:01;",
                StringTemplate.Render("{foreach Outer}{index}:{foreach Inner}{index}{/foreach Inner};{/foreach Outer}", data));
        }

        [Fact]
        public void ItemPropertyWinsOverLoopMetadata()
        {
            // An item that already has a 'count' property keeps its own value; loop metadata does not clobber it.
            var data = new { Items = new[] { new { count = 99 } } };
            Assert.Equal("99", StringTemplate.Render("{foreach Items}{count}{/foreach Items}", data));
        }
    }

    public class BaseEntity
    {
        public int Id { get; set; }
        public string Kind => "base";
    }

    public class DerivedEntity : BaseEntity
    {
        public string Name { get; set; }
    }

    public class ShadowBase
    {
        public string Label { get; set; } = "base";
    }

    public class ShadowDerived : ShadowBase
    {
        public new string Label { get; set; } = "derived";
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

    internal static class TestModuleInitializer
    {
        // Pin the test run to en-US so the fixtures' hardcoded en-US dates/numbers render identically across
        // CI runners (Linux/macOS otherwise default to an invariant / C.UTF-8 culture).
        [ModuleInitializer]
        internal static void Init()
        {
            var enUs = CultureInfo.GetCultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentCulture = enUs;
            CultureInfo.DefaultThreadCurrentUICulture = enUs;
        }
    }
}
