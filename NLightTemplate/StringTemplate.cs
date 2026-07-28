using Microsoft.CSharp.RuntimeBinder;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
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
        private static readonly StringTemplateConfiguration _cfg = new StringTemplateConfiguration();
        /// <summary>
        /// The global <see cref="FluentStringTemplateConfiguration"/>
        /// </summary>
        public static FluentStringTemplateConfiguration Configure { get; private set; } = new FluentStringTemplateConfiguration(_cfg);
        /// <summary>
        /// Renders a string template using the supplied object
        /// </summary>
        /// <param name="template">the template</param>
        /// <param name="obj">any POCO</param>
        /// <returns></returns>
        public static string Render(string template, object obj) => ReplaceText(template, BuildPropertyDictionary(obj), _cfg);
        /// <summary>
        /// Renders a string template using the supplied object
        /// </summary>
        /// <param name="template">the template</param>
        /// <param name="obj">any POCO</param>
        /// <param name="cfg">override configuration</param>
        /// <returns></returns>
        public static string Render(string template, object obj, StringTemplateConfiguration cfg) => ReplaceText(template, BuildPropertyDictionary(obj), cfg);
        /// <summary>
        /// Renders a string template using the supplied object
        /// </summary>
        /// <param name="template">the template</param>
        /// <param name="obj">any POCO</param>
        /// <param name="replacements">additional dictionary of replacement values</param>
        /// <returns></returns>
        public static string Render(string template, object obj, Dictionary<string, object> replacements) => ReplaceText(template, BuildPropertyDictionary(obj).Union(replacements).ToDictionary(x => x.Key, x => x.Value), _cfg);
        /// <summary>
        /// Renders a string template using the supplied object
        /// </summary>
        /// <param name="template">the template</param>
        /// <param name="obj">any POCO</param>
        /// <param name="replacements">additional dictionary of replacement values</param>
        /// <param name="cfg">override configuration</param>
        /// <returns></returns>
        public static string Render(string template, object obj, Dictionary<string, object> replacements, StringTemplateConfiguration cfg)
            => ReplaceText(template, BuildPropertyDictionary(obj).Union(replacements).ToDictionary(x => x.Key, x => x.Value), cfg);
        /// <summary>
        /// Renders a string template using the supplied object
        /// </summary>
        /// <param name="template">the template</param>
        /// <param name="replacements">dictionary of replacement values</param>
        /// <returns></returns>
        public static string Render(string template, Dictionary<string, object> replacements) => ReplaceText(template, replacements, _cfg);
        /// <summary>
        /// Renders a string template using the supplied object
        /// </summary>
        /// <param name="template">the template</param>
        /// <param name="replacements">dictionary of replacement values</param>
        /// <param name="cfg">override configuration</param>
        /// <returns></returns>
        public static string Render(string template, Dictionary<string, object> replacements, StringTemplateConfiguration cfg) => ReplaceText(template, replacements, cfg);
        /// <summary>
        /// Builds a property dictionary of key:value from the object instance
        /// </summary>
        /// <param name="obj">the object instance</param>
        /// <returns></returns>
        public static Dictionary<string, object> BuildPropertyDictionary(object obj)
        {
            if (obj is IDynamicMetaObjectProvider)
            {
                return BuildDynamicPropertyDictionary(obj);
            }

            string prefix(string p) => string.IsNullOrEmpty(p) ? "" : $"{p}.";

            IEnumerable<KeyValuePair<string, object>> CollectProperties(string pre, object o) =>
                PublicProperties(o.GetType())
                    .SelectMany(prop => new[] { new KeyValuePair<string, object>($"{prefix(pre)}{prop.Name}", prop.GetValue(o)) }
                    .Concat((prop.PropertyType.GetTypeInfo().IsClass && prop.PropertyType != typeof(string) && !typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(prop.PropertyType.GetTypeInfo())) ?
                        CollectProperties($"{prefix(pre)}{prop.Name}", prop.GetValue(o))
                        .Select(kvp => new KeyValuePair<string, object>($"{prefix(pre)}{kvp.Key}", kvp.Value)) : new KeyValuePair<string, object>[0]));

            return CollectProperties(string.Empty, obj).ToDictionary(x => x.Key, x => x.Value);
        }

        /// <summary>
        /// Returns the public-getter properties of a type, including those inherited from base classes. When a
        /// property is hidden (<c>new</c>) or overridden, the most-derived declaration wins, so each name appears once.
        /// </summary>
        private static IEnumerable<PropertyInfo> PublicProperties(Type type)
        {
            var seen = new HashSet<string>();
            for (var current = type; current != null && current != typeof(object); current = current.GetTypeInfo().BaseType)
                foreach (var property in current.GetTypeInfo().DeclaredProperties)
                    if ((property.GetMethod?.IsPublic ?? false) && seen.Add(property.Name))
                        yield return property;
        }

        /// <summary>
        /// Builds a property dictionary of key:value from the object instance
        /// </summary>
        /// <param name="obj">the dynamic object instance</param>
        /// <returns></returns>
        public static Dictionary<string, object> BuildDynamicPropertyDictionary(dynamic obj)
        {
            string prefix(string p) => string.IsNullOrEmpty(p) ? "" : $"{p}.";

            IEnumerable<KeyValuePair<string, object>> CollectProperties(string pre, dynamic o)
            {
                Dictionary<string, object> casted;
                try
                {
                    casted = new Dictionary<string, object>(o);
                }
                catch (RuntimeBinderException)
                {
                    try
                    {
                        var t = o.Type;
                        if (t?.ToString() == nameof(Array))
                        {
                            Dictionary<string, object>[] oo = o.ToObject<Dictionary<string, object>[]>();

                            return new[] {new KeyValuePair<string, object>(pre,oo.Select(f => f
                                .Select(kvp => kvp.Value.GetType().GetTypeInfo().IsClass
                                    ? CollectProperties($"{prefix(pre)}{kvp.Key}", kvp.Value)
                                    : new[] { new KeyValuePair<string, object>($"{prefix(pre)}{kvp.Key}", kvp.Value) }
                                )
                            ))};
                        }
                        else
                        {
                            casted = o.ToObject<Dictionary<string, object>>();
                        }
                    }
                    catch (RuntimeBinderException)
                    {
                        casted = o.ToObject<Dictionary<string, object>>();
                    }
                }
                return casted
                    .SelectMany(prop => new[] { new KeyValuePair<string, object>($"{prefix(pre)}{prop.Key}", prop.Value) }
                        .Concat((prop.Value is IDynamicMetaObjectProvider prov && ((dynamic)prov).Type?.ToString() != nameof(Array)) ? CollectProperties($"{prefix(pre)}{prop.Key}", prop.Value)
                        .Select(kvp => new KeyValuePair<string, object>($"{prefix(pre)}{kvp.Key}", kvp.Value)) : new KeyValuePair<string, object>[0])
                    );

            }
            return CollectProperties(string.Empty, obj).ToDictionary(x => x.Key, x => x.Value);
        }

        /// <summary>
        /// This performs all of the token replacements and recursion
        /// </summary>
        /// <param name="text">The snippet to process for the supplied replacements</param>
        /// <param name="replacements">The replacements</param>
        /// <param name="cfg">The configuration</param>
        /// <returns></returns>
        internal static string ReplaceText(string text, Dictionary<string, object> replacements, StringTemplateConfiguration cfg) =>
            replacements.ToList().OrderBy((kvp) => (kvp.Value is IEnumerable && kvp.Value.GetType() != typeof(string)) ? 1 : 2).Aggregate(text, (c, k) =>
                (k.Value is IEnumerable enumerable && !(k.Value is string) && c.IndexOf($"{cfg.OpenToken}{cfg.ForeachToken} {k.Key}{cfg.CloseToken}") >= 0 && c.IndexOf($"{cfg.OpenToken}/{cfg.ForeachToken} {k.Key}{cfg.CloseToken}") > 0) ?
                    GetRegex(string.Format(
                            @"{0}(?<inner>(?>{0}(?<LEVEL>)|{1}(?<-LEVEL>)|(?!{0}|{1}).)+(?(LEVEL)(?!))){1}",
                            Escape($"{cfg.OpenToken}{cfg.ForeachToken} {k.Key}{cfg.CloseToken}"),
                            Escape($"{cfg.OpenToken}/{cfg.ForeachToken} {k.Key}{cfg.CloseToken}")
                            ),
                        RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline)
                    .Matches(text).Cast<Match>().Aggregate(c, (prev, match) => prev.Replace(match.Captures[0].Value,
                        string.Join("", WithLoopMetadata(enumerable).Select(scope => ReplaceText(match.Groups[1].Value, scope, cfg)))))
                :
                ReplaceToken(c, k.Key, k.Value, cfg, replacements)
            );

        /// <summary>
        /// Builds the per-item property dictionaries for a <c>foreach</c> body, adding loop metadata:
        /// <c>index</c> (0-based), <c>first</c>/<c>last</c> (bool), and <c>count</c>. A property already on the item
        /// with one of those names is left untouched (the item's own value wins).
        /// </summary>
        private static IEnumerable<Dictionary<string, object>> WithLoopMetadata(IEnumerable enumerable)
        {
            var items = enumerable.Cast<object>().ToList();
            for (var i = 0; i < items.Count; i++)
            {
                var scope = BuildPropertyDictionary(items[i]);
                AddIfAbsent(scope, "index", i);
                AddIfAbsent(scope, "first", i == 0);
                AddIfAbsent(scope, "last", i == items.Count - 1);
                AddIfAbsent(scope, "count", items.Count);
                yield return scope;
            }
        }

        private static void AddIfAbsent(Dictionary<string, object> dict, string key, object value)
        {
            if (!dict.ContainsKey(key)) dict[key] = value;
        }

        internal static string ReplaceToken(string original, string key, object value, StringTemplateConfiguration cfg, Dictionary<string, object> replacements)
        {
            var typeInfo = value?.GetType().GetTypeInfo();
            var toStringMethod = (typeInfo?.IsEnum ?? false ? typeInfo?.BaseType.GetTypeInfo() : typeInfo)?
                .GetDeclaredMethods("ToString")
                .FirstOrDefault(p =>
                    p.GetParameters().Select(q => q.ParameterType).SequenceEqual(new Type[] { typeof(string) })
                );

            original = ReplaceConditionals(original, key, value, cfg, replacements);
            original = original.Replace($"{cfg.OpenToken}{key}{cfg.CloseToken}", value?.ToString() ?? string.Empty);

            // Escape the tokens and key (they can contain regex metacharacters, and dotted keys contain '.'),
            // and match the format up to the close token rather than a hard-coded '}'.
            var closeToken = Regex.Escape(cfg.CloseToken);
            var formatPattern = $@"{Regex.Escape(cfg.OpenToken)}(?<key>{Regex.Escape(key)})(,(?<pad>-*?\d+))*?(:(?<fmt>(?:(?!{closeToken}).)+))*?{closeToken}";

            return GetRegex(formatPattern).Matches(original)
                .Cast<Match>()
                .Aggregate(original, (s, match) =>
            {
                var v = toStringMethod == null ? value?.ToString() : toStringMethod.Invoke(value, new[] { match.Groups["fmt"]?.Value ?? string.Empty }) as string;
                if (int.TryParse(match.Groups["pad"]?.Value ?? string.Empty, out int padding))
                {
                    v = padding < 0 ? v.PadRight(Math.Abs(padding)) : v.PadLeft(Math.Abs(padding));
                }
                return s.Replace(match.Value, v);
            });
        }

        /// <summary>
        /// Escapes a token literal into a sequence of <c>\uXXXX</c> escapes so tokens containing
        /// regex metacharacters can be embedded safely in a pattern.
        /// </summary>
        private static string Escape(string token) => string.Join("", token.ToCharArray().Select(ch => $"\\u{(int)ch:X4}"));

        /// <summary>
        /// Cache of <see cref="Regex"/> instances keyed by pattern and options. The relatively expensive pattern
        /// parsing happens once per distinct pattern and is reused across renders.
        /// </summary>
        private static readonly ConcurrentDictionary<(string Pattern, RegexOptions Options), Regex> _regexCache = new ConcurrentDictionary<(string Pattern, RegexOptions Options), Regex>();

        /// <summary>
        /// Returns a cached <see cref="Regex"/> for the pattern/options, building it on first use.
        /// </summary>
        private static Regex GetRegex(string pattern, RegexOptions options = RegexOptions.None) => _regexCache.GetOrAdd((pattern, options), key => new Regex(key.Pattern, key.Options));

        /// <summary>
        /// Resolves <c>{if Key}</c>/<c>{if Key op value}</c> conditional blocks (with optional <c>{else}</c>) for the supplied key.
        /// Boolean-only <c>{if Key}</c> blocks keep their original behaviour; comparison operators (==, !=, &gt;, &lt;, &gt;=, &lt;=)
        /// evaluate <paramref name="value"/> against the right-hand side. The right-hand side is a literal by default, or another
        /// property's value when prefixed with <c>@</c> (e.g. <c>{if Total &gt;= @Minimum}</c>); enum values match by name
        /// (case-insensitively) or by numeric value.
        /// </summary>
        /// <param name="original">The snippet to process</param>
        /// <param name="key">The property key being evaluated</param>
        /// <param name="value">The property value</param>
        /// <param name="cfg">The configuration</param>
        /// <param name="replacements">All flattened replacements in scope, used to resolve <c>@</c> property references</param>
        /// <returns></returns>
        internal static string ReplaceConditionals(string original, string key, object value, StringTemplateConfiguration cfg, Dictionary<string, object> replacements)
        {
            if (original.IndexOf($"{cfg.OpenToken}{cfg.IfToken} {key}", StringComparison.Ordinal) < 0
                || original.IndexOf($"{cfg.OpenToken}/{cfg.IfToken} {key}{cfg.CloseToken}", StringComparison.Ordinal) < 0)
                return original;

            var openPrefix = Escape($"{cfg.OpenToken}{cfg.IfToken} {key}");
            var closeTag = Escape($"{cfg.OpenToken}/{cfg.IfToken} {key}{cfg.CloseToken}");
            var closeTok = Escape(cfg.CloseToken);
            // after the key there must be no further identifier char (so "Age" never matches "AgeGroup"); the optional
            // condition is any run of characters up to the close token.
            var boundary = $@"(?![\p{{L}}\p{{Nd}}_.])";
            var openCap = $@"{openPrefix}{boundary}(?<cond>(?:(?!{closeTok}).)*){closeTok}";
            var openNc = $@"{openPrefix}{boundary}(?:(?!{closeTok}).)*{closeTok}";

            var rx = GetRegex(
                $@"{openCap}(?<inner>(?>{openNc}(?<LEVEL>)|{closeTag}(?<-LEVEL>)|(?!{openNc}|{closeTag}).)+(?(LEVEL)(?!))){closeTag}",
                RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);

            return rx.Matches(original).Cast<Match>().Aggregate(original, (acc, match) =>
            {
                var outcome = EvaluateCondition(value, match.Groups["cond"].Value.Trim(), replacements);
                if (!outcome.HasValue) return acc; // undecidable (boolean form on a non-bool, or unparseable) -> leave verbatim
                SplitElse(match.Groups["inner"].Value, cfg, out string truePart, out string elsePart);
                return acc.Replace(match.Captures[0].Value, ReplaceConditionals(outcome.Value ? truePart : elsePart, key, value, cfg, replacements));
            });
        }

        /// <summary>
        /// Splits an <c>{if}</c> block's inner content on its own (depth-0) <c>{else}</c> marker, skipping any
        /// <c>{else}</c> that belongs to a nested <c>{if}</c> block.
        /// </summary>
        private static void SplitElse(string inner, StringTemplateConfiguration cfg, out string truePart, out string elsePart)
        {
            truePart = inner;
            elsePart = string.Empty;
            var rx = GetRegex($@"(?<ifc>{Escape($"{cfg.OpenToken}/{cfg.IfToken} ")})|(?<ifo>{Escape($"{cfg.OpenToken}{cfg.IfToken} ")})|(?<els>{Escape($"{cfg.OpenToken}{cfg.ElseToken}{cfg.CloseToken}")})");
            var depth = 0;
            foreach (Match m in rx.Matches(inner))
            {
                if (m.Groups["ifo"].Success) depth++;
                else if (m.Groups["ifc"].Success) { if (depth > 0) depth--; }
                else if (depth == 0)
                {
                    truePart = inner.Substring(0, m.Index);
                    elsePart = inner.Substring(m.Index + m.Length);
                    return;
                }
            }
        }

        /// <summary>
        /// Evaluates a condition expression for the supplied value. Returns <c>null</c> when the block should be
        /// left untouched: an operator-less block (<c>{if Key}</c>) whose value is not a <see cref="bool"/>, or an
        /// expression with no recognised operator.
        /// </summary>
        private static bool? EvaluateCondition(object value, string cond, Dictionary<string, object> replacements)
        {
            if (cond.Length == 0) return value is bool b ? b : (bool?)null;

            var op = new[] { "==", "!=", ">=", "<=" }.FirstOrDefault(o => cond.StartsWith(o, StringComparison.Ordinal))
                     ?? new[] { ">", "<" }.FirstOrDefault(o => cond.StartsWith(o, StringComparison.Ordinal));
            if (op == null) return null;

            return Compare(value, op, ResolveRightHandSide(cond.Substring(op.Length).Trim(), replacements));
        }

        /// <summary>
        /// Resolves the right-hand side of a condition: an <c>@</c>-prefixed token is looked up in
        /// <paramref name="replacements"/> as another property (its value, or <c>null</c> when absent); anything else
        /// is the literal token text.
        /// </summary>
        private static object ResolveRightHandSide(string rhsToken, Dictionary<string, object> replacements) =>
            rhsToken.Length > 1 && rhsToken[0] == '@'
                ? (replacements != null && replacements.TryGetValue(rhsToken.Substring(1).Trim(), out var v) ? v : null)
                : rhsToken;

        /// <summary>
        /// Type-aware comparison of <paramref name="value"/> against the right-hand side operand: enum-aware when the
        /// value is an <see cref="Enum"/> (by name, case-insensitively, or by numeric value), numeric when both sides
        /// are numeric, boolean when the value is a <see cref="bool"/> and the operand is true/false, otherwise ordinal string.
        /// </summary>
        private static bool Compare(object value, string op, object rhs)
        {
            if (value is Enum && TryResolveEnum(value.GetType(), rhs, out var enumRhs))
                return CompareDoubles(Convert.ToDouble(value, CultureInfo.InvariantCulture), op, Convert.ToDouble(enumRhs, CultureInfo.InvariantCulture));

            if (value is bool bv && TryToBool(rhs, out bool rb))
                return op == "==" ? bv == rb : op == "!=" && bv != rb;

            if (TryToDouble(value, out double dv) && TryToDouble(rhs, out double dr))
                return CompareDoubles(dv, op, dr);

            var sv = value?.ToString() ?? string.Empty;
            var sr = rhs?.ToString() ?? string.Empty;
            switch (op)
            {
                case "==": return string.Equals(sv, sr, StringComparison.Ordinal);
                case "!=": return !string.Equals(sv, sr, StringComparison.Ordinal);
                case ">": return string.CompareOrdinal(sv, sr) > 0;
                case "<": return string.CompareOrdinal(sv, sr) < 0;
                case ">=": return string.CompareOrdinal(sv, sr) >= 0;
                default: return string.CompareOrdinal(sv, sr) <= 0;
            }
        }

        /// <summary>
        /// Applies a comparison operator to two <see cref="double"/> operands.
        /// </summary>
        private static bool CompareDoubles(double left, string op, double right)
        {
            switch (op)
            {
                case "==": return left == right;
                case "!=": return left != right;
                case ">": return left > right;
                case "<": return left < right;
                case ">=": return left >= right;
                default: return left <= right;
            }
        }

        /// <summary>
        /// Attempts to interpret the right-hand operand as a <see cref="bool"/> — either a boxed <see cref="bool"/> or
        /// the literal text "true"/"false" (case-insensitively).
        /// </summary>
        private static bool TryToBool(object rhs, out bool result)
        {
            result = false;
            if (rhs is bool b) { result = b; return true; }
            if (rhs is string s && (string.Equals(s, "true", StringComparison.OrdinalIgnoreCase) || string.Equals(s, "false", StringComparison.OrdinalIgnoreCase)))
            {
                result = string.Equals(s, "true", StringComparison.OrdinalIgnoreCase);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Attempts to resolve the right-hand operand into a value of <paramref name="enumType"/>: an existing enum
        /// instance, a member name (case-insensitive) or numeric string, or a boxed numeric value.
        /// </summary>
        private static bool TryResolveEnum(Type enumType, object rhs, out object result)
        {
            result = null;
            if (rhs == null) return false;
            if (rhs.GetType() == enumType) { result = rhs; return true; }
            if (rhs is string s)
            {
                try { result = Enum.Parse(enumType, s.Trim(), ignoreCase: true); return true; }
                catch { return false; }
            }
            if (TryToDouble(rhs, out double d))
            {
                try { result = Enum.ToObject(enumType, Convert.ChangeType(d, Enum.GetUnderlyingType(enumType), CultureInfo.InvariantCulture)); return true; }
                catch { return false; }
            }
            return false;
        }

        /// <summary>
        /// Attempts to convert a value to <see cref="double"/> using the invariant culture. <see cref="bool"/> and
        /// non-numeric values return <c>false</c> so they fall through to boolean or string comparison.
        /// </summary>
        private static bool TryToDouble(object value, out double result)
        {
            result = 0d;
            if (value == null || value is bool) return false;
            try { result = Convert.ToDouble(value, CultureInfo.InvariantCulture); return true; }
            catch { return false; }
        }
    }
}
