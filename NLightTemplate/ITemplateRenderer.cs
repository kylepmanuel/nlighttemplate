using System.Collections.Generic;

namespace NLightTemplate
{
    /// <summary>
    /// Instance-based renderer abstraction for <see cref="StringTemplate"/>, suitable for
    /// dependency injection. Each instance carries its own <see cref="StringTemplateConfiguration"/>.
    /// </summary>
    public interface ITemplateRenderer
    {
        /// <summary>
        /// Renders a string template using the supplied object
        /// </summary>
        /// <param name="template">the template</param>
        /// <param name="obj">any POCO</param>
        /// <returns></returns>
        string Render(string template, object obj);
        /// <summary>
        /// Renders a string template using the supplied object
        /// </summary>
        /// <param name="template">the template</param>
        /// <param name="obj">any POCO</param>
        /// <param name="replacements">additional dictionary of replacement values</param>
        /// <returns></returns>
        string Render(string template, object obj, Dictionary<string, object> replacements);
        /// <summary>
        /// Renders a string template using the supplied replacements
        /// </summary>
        /// <param name="template">the template</param>
        /// <param name="replacements">dictionary of replacement values</param>
        /// <returns></returns>
        string Render(string template, Dictionary<string, object> replacements);
    }

    /// <summary>
    /// Default <see cref="ITemplateRenderer"/> implementation. Delegates to the static
    /// <see cref="StringTemplate"/> engine using the configuration supplied at construction.
    /// </summary>
    public class TemplateRenderer : ITemplateRenderer
    {
        private readonly StringTemplateConfiguration _cfg;
        /// <summary>
        /// Constructs a new <see cref="TemplateRenderer"/> using the supplied <see cref="IStringTemplateConfiguration"/>
        /// (or the default configuration when none is supplied). The parameter is optional so the renderer can be
        /// registered with a container as <c>AddSingleton&lt;ITemplateRenderer, TemplateRenderer&gt;()</c> — an
        /// <see cref="IStringTemplateConfiguration"/> is injected when one is registered, otherwise defaults are used.
        /// </summary>
        /// <param name="cfg">The configuration; defaults to a new <see cref="StringTemplateConfiguration"/></param>
        public TemplateRenderer(IStringTemplateConfiguration cfg = null) => _cfg = AsConfiguration(cfg);
        /// <inheritdoc/>
        public string Render(string template, object obj) => StringTemplate.Render(template, obj, _cfg);
        /// <inheritdoc/>
        public string Render(string template, object obj, Dictionary<string, object> replacements) => StringTemplate.Render(template, obj, replacements, _cfg);
        /// <inheritdoc/>
        public string Render(string template, Dictionary<string, object> replacements) => StringTemplate.Render(template, replacements, _cfg);

        /// <summary>
        /// Normalizes any <see cref="IStringTemplateConfiguration"/> into the concrete <see cref="StringTemplateConfiguration"/>
        /// the static engine consumes: the instance itself when it already is one, otherwise a snapshot of its tokens.
        /// </summary>
        private static StringTemplateConfiguration AsConfiguration(IStringTemplateConfiguration cfg) =>
            cfg == null ? new StringTemplateConfiguration()
            : cfg as StringTemplateConfiguration
              ?? new StringTemplateConfiguration
              {
                  OpenToken = cfg.OpenToken,
                  CloseToken = cfg.CloseToken,
                  ForeachToken = cfg.ForeachToken,
                  IfToken = cfg.IfToken,
                  ElseToken = cfg.ElseToken
              };
    }
}
