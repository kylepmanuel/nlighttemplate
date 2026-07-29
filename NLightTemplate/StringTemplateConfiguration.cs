using System;
using System.Collections.Generic;
using System.Text;

namespace NLightTemplate
{
    /// <summary>
    /// Read-only view of the <see cref="StringTemplate"/> token configuration. Useful as a dependency-injection
    /// abstraction; the concrete <see cref="StringTemplateConfiguration"/> implements it.
    /// </summary>
    public interface IStringTemplateConfiguration
    {
        /// <summary>The Open token (default "{")</summary>
        string OpenToken { get; }
        /// <summary>The Close token (default "}")</summary>
        string CloseToken { get; }
        /// <summary>The Foreach token (default "foreach")</summary>
        string ForeachToken { get; }
        /// <summary>The If token (default "if")</summary>
        string IfToken { get; }
        /// <summary>The Else token (default "else")</summary>
        string ElseToken { get; }
        /// <summary>Format provider for format specifiers (default null = current culture)</summary>
        IFormatProvider FormatProvider { get; }
        /// <summary>When true, HTML-encodes substituted values (default false)</summary>
        bool HtmlEncode { get; }
        /// <summary>When true, trims whitespace and newlines around block tags (default false)</summary>
        bool TrimBlockWhitespace { get; }
    }

    /// <summary>
    /// Configuration options for <see cref="StringTemplate"/>
    /// </summary>
    public class StringTemplateConfiguration : IStringTemplateConfiguration
    {
        /// <summary>
        /// The Open token (default "{")
        /// </summary>
        public string OpenToken { get; set; } = "{";
        /// <summary>
        /// The Close token (default "}")
        /// </summary>
        public string CloseToken { get; set; } = "}";
        /// <summary>
        /// The Foreach token (default "foreach")
        /// </summary>
        public string ForeachToken { get; set; } = "foreach";
        /// <summary>
        /// The If token (default "if")
        /// </summary>
        public string IfToken { get; set; } = "if";
        /// <summary>
        /// The Else token (default "else")
        /// </summary>
        public string ElseToken { get; set; } = "else";
        /// <summary>
        /// The <see cref="IFormatProvider"/> (for example a <see cref="System.Globalization.CultureInfo"/>) used for
        /// format specifiers. Default null uses the current culture.
        /// </summary>
        public IFormatProvider FormatProvider { get; set; }
        /// <summary>
        /// When true, HTML-encodes the substituted values (not the template literals) for safe HTML/email output.
        /// Default false.
        /// </summary>
        public bool HtmlEncode { get; set; }
        /// <summary>
        /// When true, trims horizontal whitespace before, and a single trailing newline after, each block tag
        /// (<c>foreach</c>, <c>if</c>, <c>else</c>) so control tags on their own line do not leave blank lines.
        /// Default false.
        /// </summary>
        public bool TrimBlockWhitespace { get; set; }

        /// <summary>
        /// Builds a <see cref="StringTemplateConfiguration"/> using the fluent interface. Convenient for
        /// dependency-injection registration, e.g. <c>services.AddSingleton&lt;IStringTemplateConfiguration&gt;(_ =&gt;
        /// StringTemplateConfiguration.Create(c =&gt; c.OpenToken("&lt;%").CloseToken("%&gt;")))</c>.
        /// </summary>
        /// <param name="configure">Callback that configures the tokens via the fluent interface</param>
        /// <returns>The configured <see cref="StringTemplateConfiguration"/></returns>
        public static StringTemplateConfiguration Create(Action<FluentStringTemplateConfiguration> configure)
        {
            var cfg = new StringTemplateConfiguration();
            configure?.Invoke(new FluentStringTemplateConfiguration(cfg));
            return cfg;
        }
    }

    /// <summary>
    /// Fluent configuration interface for <see cref="StringTemplate"/>'s <see cref="StringTemplateConfiguration"/>
    /// </summary>
    public class FluentStringTemplateConfiguration
    {
        private StringTemplateConfiguration _cfg = new StringTemplateConfiguration();
        /// <summary>
        /// Default constructor
        /// </summary>
        public FluentStringTemplateConfiguration() { }

        /// <summary>
        /// Constructs a new <see cref="FluentStringTemplateConfiguration"/> instance using the supplied <see cref="StringTemplateConfiguration"/>
        /// </summary>
        /// <param name="cfg">The configuration</param>
        public FluentStringTemplateConfiguration(StringTemplateConfiguration cfg)
        {
            _cfg = cfg;
        }
        /// <summary>
        /// Sets the Open Token <see cref="StringTemplateConfiguration.OpenToken"/>
        /// </summary>
        /// <param name="openToken">The Open Token</param>
        /// <returns></returns>
        public FluentStringTemplateConfiguration OpenToken(string openToken)
        {
            _cfg.OpenToken = openToken;
            return this;
        }
        /// <summary>
        /// Sets the Close Token
        /// </summary>
        /// <param name="closeToken">The Close Token <see cref="StringTemplateConfiguration.CloseToken"/></param>
        /// <returns></returns>
        public FluentStringTemplateConfiguration CloseToken(string closeToken)
        {
            _cfg.CloseToken = closeToken;
            return this;
        }
        /// <summary>
        /// Sets the Foreach Token <see cref="StringTemplateConfiguration.ForeachToken"/>
        /// </summary>
        /// <param name="foreachToken">The Foreach Token</param>
        /// <returns></returns>
        public FluentStringTemplateConfiguration ForeachToken(string foreachToken)
        {
            _cfg.ForeachToken = foreachToken;
            return this;
        }
        /// <summary>
        /// Sets te If Token <see cref="StringTemplateConfiguration.IfToken"/>
        /// </summary>
        /// <param name="ifToken">The If Token</param>
        /// <returns></returns>
        public FluentStringTemplateConfiguration IfToken(string ifToken)
        {
            _cfg.IfToken = ifToken;
            return this;
        }
        /// <summary>
        /// Sets the Else Token <see cref="StringTemplateConfiguration.ElseToken"/>
        /// </summary>
        /// <param name="elseToken">The Else Token</param>
        /// <returns></returns>
        public FluentStringTemplateConfiguration ElseToken(string elseToken)
        {
            _cfg.ElseToken = elseToken;
            return this;
        }
        /// <summary>
        /// Sets the <see cref="StringTemplateConfiguration.FormatProvider"/> (for example a
        /// <see cref="System.Globalization.CultureInfo"/>) used for format specifiers.
        /// </summary>
        /// <param name="formatProvider">The format provider</param>
        /// <returns></returns>
        public FluentStringTemplateConfiguration FormatProvider(IFormatProvider formatProvider)
        {
            _cfg.FormatProvider = formatProvider;
            return this;
        }
        /// <summary>
        /// Enables (or disables) HTML-encoding of substituted values (<see cref="StringTemplateConfiguration.HtmlEncode"/>).
        /// </summary>
        /// <param name="htmlEncode">Whether to HTML-encode values</param>
        /// <returns></returns>
        public FluentStringTemplateConfiguration HtmlEncode(bool htmlEncode = true)
        {
            _cfg.HtmlEncode = htmlEncode;
            return this;
        }
        /// <summary>
        /// Enables (or disables) trimming of whitespace and newlines around block tags
        /// (<see cref="StringTemplateConfiguration.TrimBlockWhitespace"/>).
        /// </summary>
        /// <param name="trim">Whether to trim block-tag whitespace</param>
        /// <returns></returns>
        public FluentStringTemplateConfiguration TrimBlockWhitespace(bool trim = true)
        {
            _cfg.TrimBlockWhitespace = trim;
            return this;
        }
        /// <summary>
        /// Exposes the internal <see cref="StringTemplateConfiguration"/>
        /// </summary>
        /// <returns></returns>
        public StringTemplateConfiguration ExposeConfiguration()
        {
            return _cfg;
        }
    }
}
