using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace NLightTemplate
{
    /// <summary>
    /// Lightweight string templating class
    /// </summary>
    public static class StringTemplate
    {
        /// <summary>
        /// Renders a string template using the supplied object
        /// </summary>
        /// <param name="template">the template</param>
        /// <param name="obj">any POCO</param>
        /// <returns></returns>
        public static string Render(string template, object obj) => ReplaceText(template, BuildPropertyDictionary(obj));
        /// <summary>
        /// Renders a string template using the supplied object
        /// </summary>
        /// <param name="template">the template</param>
        /// <param name="obj">any POCO</param>
        /// <param name="replacements">additional dictionary of replacement values</param>
        /// <returns></returns>
        public static string Render(string template, object obj, Dictionary<string, object> replacements) => ReplaceText(template, BuildPropertyDictionary(obj).Union(replacements).ToDictionary(x => x.Key, x => x.Value));
        /// <summary>
        /// Renders a string template using the supplied object
        /// </summary>
        /// <param name="template">the template</param>
        /// <param name="replacements">dictionary of replacement values</param>
        /// <returns></returns>
        public static string Render(string template, Dictionary<string, object> replacements) => ReplaceText(template, replacements);

        /// <summary>
        /// Builds a property dictionary of key:value from the object instance
        /// </summary>
        /// <param name="obj">the object instance</param>
        /// <returns></returns>
        public static Dictionary<string, object> BuildPropertyDictionary(object obj)
        {
            string prefix(string p) => string.IsNullOrEmpty(p) ? "" : $"{p}.";

            IEnumerable<KeyValuePair<string, object>> CollectProperties(string pre, object o) =>
                o.GetType().GetTypeInfo().DeclaredProperties.Where(p => p.CanRead)
                    .SelectMany(prop => new[] { new KeyValuePair<string, object>($"{prefix(pre)}{prop.Name}", prop.GetValue(o)) }
                    .Concat((prop.PropertyType.GetTypeInfo().IsClass && prop.PropertyType != typeof(string) && !typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(prop.PropertyType.GetTypeInfo())) ?
                        CollectProperties($"{prefix(pre)}{prop.Name}", prop.GetValue(o))
                        .Select(kvp => new KeyValuePair<string, object>($"{prefix(pre)}{kvp.Key}", kvp.Value)) : new KeyValuePair<string, object>[0]));

            return CollectProperties(string.Empty, obj).ToDictionary(x => x.Key, x => x.Value);
        }

        internal static string ReplaceText(string text, Dictionary<string, object> replacements) =>
            replacements.ToList().OrderBy((kvp) => (kvp.Value is IEnumerable && kvp.Value.GetType() != typeof(string)) ? 1 : 2).Aggregate(text, (c, k) =>
                (k.Value is IEnumerable enumerable && !(k.Value is string) && c.IndexOf($"{{foreach {k.Key}}}") >= 0 && c.IndexOf($"{{/foreach {k.Key}}}") > 0) ?
                    new Regex(string.Format(
                            @"{0}(?<inner>(?>{0}(?<LEVEL>)|{1}(?<-LEVEL>)|(?!{0}|{1}).)+(?(LEVEL)(?!))){1}",
                            $@"{{foreach\s{k.Key}}}",
                            $@"{{/foreach\s{k.Key}}}"
                            ),
                        RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline)
                    .Matches(text).Cast<Match>().Aggregate(c, (prev, match) => prev.Replace(match.Captures[0].Value,
                        string.Join("", enumerable.Cast<object>().Select(item => ReplaceText(match.Groups[1].Value, BuildPropertyDictionary(item))))))
                :
                c.Replace($"{{{k.Key}}}", k.Value?.ToString() ?? string.Empty)
            );
    }
}
