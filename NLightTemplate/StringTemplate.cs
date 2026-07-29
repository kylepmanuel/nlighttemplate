using Microsoft.CSharp.RuntimeBinder;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
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
                Accessors(o.GetType()).SelectMany(a =>
                {
                    var name = $"{prefix(pre)}{a.Name}";
                    var value = a.Getter(o);
                    var head = new[] { new KeyValuePair<string, object>(name, value) };
                    return a.Recurse && value != null ? head.Concat(CollectProperties(name, value)) : head;
                });

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
        /// Per-type cache of compiled property accessors: each entry is the property name, a compiled getter
        /// delegate (far faster than reflection on repeat renders), and whether to recurse for dot notation.
        /// Keyed by <see cref="Type"/>, so it is bounded by the number of distinct POCO types the app renders
        /// (dynamic/<see cref="System.Dynamic.ExpandoObject"/> inputs use a different path and never populate this).
        /// </summary>
        private static readonly ConcurrentDictionary<Type, (string Name, Func<object, object> Getter, bool Recurse)[]> _accessorCache
            = new ConcurrentDictionary<Type, (string Name, Func<object, object> Getter, bool Recurse)[]>();

        private static (string Name, Func<object, object> Getter, bool Recurse)[] Accessors(Type type) =>
            _accessorCache.GetOrAdd(type, t => PublicProperties(t).Select(p =>
            {
                var pt = p.PropertyType;
                var recurse = pt.GetTypeInfo().IsClass && pt != typeof(string) && !typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(pt.GetTypeInfo());
                return (p.Name, CompileGetter(p), recurse);
            }).ToArray());

        /// <summary>
        /// Compiles a property getter into a <c>Func&lt;object, object&gt;</c> via an expression tree, so subsequent
        /// reads avoid reflection.
        /// </summary>
        private static Func<object, object> CompileGetter(PropertyInfo property)
        {
            var instance = Expression.Parameter(typeof(object), "instance");
            var body = Expression.Convert(
                Expression.Property(Expression.Convert(instance, property.DeclaringType), property),
                typeof(object));
            return Expression.Lambda<Func<object, object>>(body, instance).Compile();
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
        internal static string ReplaceText(string text, Dictionary<string, object> replacements, StringTemplateConfiguration cfg)
        {
            if (cfg.TrimBlockWhitespace) text = TrimBlocks(text, cfg);

            var result =
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

            // A `{Key ?? fallback}` whose key is not present in this scope resolves to the fallback.
            return ResolveMissingCoalesce(result, cfg);
        }

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
            original = ReplaceConditionals(original, key, value, cfg, replacements);

            // Nothing else to do unless a {Key...} token (scalar, format, or ?? fallback) references this key. This
            // keeps the per-key work and the regex cache bounded by the template's tokens, not by how many data
            // properties there are (important for free-form/ExpandoObject data with many, ever-changing keys).
            if (original.IndexOf($"{cfg.OpenToken}{key}", StringComparison.Ordinal) < 0)
                return original;

            // Null-coalesce: {Key ?? fallback} renders the value, or the (optionally quoted) fallback when null.
            if (original.IndexOf("??", StringComparison.Ordinal) >= 0)
            {
                var coalescePattern = $@"{Regex.Escape(cfg.OpenToken)}{Regex.Escape(key)}\s*\?\?\s*(?<fallback>(?:(?!{Regex.Escape(cfg.CloseToken)}).)+){Regex.Escape(cfg.CloseToken)}";
                original = GetRegex(coalescePattern).Matches(original).Cast<Match>().Aggregate(original, (s, m) =>
                    s.Replace(m.Value, Encode(value == null ? Unquote(m.Groups["fallback"].Value) : FormatValue(value, string.Empty, cfg), cfg)));
            }

            original = original.Replace($"{cfg.OpenToken}{key}{cfg.CloseToken}", Encode(FormatValue(value, string.Empty, cfg), cfg));

            // Escape the tokens and key (they can contain regex metacharacters, and dotted keys contain '.'),
            // and match the format up to the close token rather than a hard-coded '}'.
            var closeToken = Regex.Escape(cfg.CloseToken);
            var formatPattern = $@"{Regex.Escape(cfg.OpenToken)}(?<key>{Regex.Escape(key)})(,(?<pad>-*?\d+))*?(:(?<fmt>(?:(?!{closeToken}).)+))*?{closeToken}";

            return GetRegex(formatPattern).Matches(original)
                .Cast<Match>()
                .Aggregate(original, (s, match) =>
            {
                var v = FormatValue(value, match.Groups["fmt"]?.Value ?? string.Empty, cfg);
                if (int.TryParse(match.Groups["pad"]?.Value ?? string.Empty, out int padding))
                {
                    v = padding < 0 ? v.PadRight(Math.Abs(padding)) : v.PadLeft(Math.Abs(padding));
                }
                return s.Replace(match.Value, Encode(v, cfg));
            });
        }

        /// <summary>
        /// Renders a value for substitution: uses the format specifier and configured <see cref="IFormatProvider"/>
        /// when the value is <see cref="IFormattable"/>, otherwise its plain <c>ToString()</c>.
        /// </summary>
        private static string FormatValue(object value, string format, StringTemplateConfiguration cfg)
        {
            if (value == null) return string.Empty;
            if (value is IFormattable formattable && (!string.IsNullOrEmpty(format) || cfg.FormatProvider != null))
                return formattable.ToString(string.IsNullOrEmpty(format) ? null : format, cfg.FormatProvider);
            return value.ToString();
        }

        /// <summary>
        /// HTML-encodes a substituted value when <see cref="StringTemplateConfiguration.HtmlEncode"/> is enabled.
        /// </summary>
        private static string Encode(string value, StringTemplateConfiguration cfg) =>
            cfg.HtmlEncode ? WebUtility.HtmlEncode(value) : value;

        /// <summary>
        /// Trims horizontal whitespace before, and a single trailing newline after, each block tag, so control tags
        /// sitting on their own line do not leave blank lines in the output.
        /// </summary>
        private static string TrimBlocks(string text, StringTemplateConfiguration cfg)
        {
            var tag = BlockTagPattern(cfg);
            text = GetRegex($@"(?m)^[ \t]+(?={tag})").Replace(text, string.Empty);
            text = GetRegex($@"({tag})[ \t]*\r?\n").Replace(text, "$1");
            return text;
        }

        /// <summary>
        /// A regex fragment matching any block control tag: <c>{foreach X}</c>/<c>{/foreach X}</c>,
        /// <c>{if X}</c>/<c>{/if X}</c>, <c>{else if X}</c>, and <c>{else}</c> (using the configured tokens). The
        /// required space after a keyword keeps a scalar like <c>{ifield}</c> from matching.
        /// </summary>
        private static string BlockTagPattern(StringTemplateConfiguration cfg)
        {
            var open = Regex.Escape(cfg.OpenToken);
            var close = Regex.Escape(cfg.CloseToken);
            var content = $@"(?:(?!{close}).)*{close}";
            var f = Regex.Escape(cfg.ForeachToken);
            var i = Regex.Escape(cfg.IfToken);
            var e = Regex.Escape(cfg.ElseToken);
            return "(?:" + string.Join("|", new[]
            {
                $@"{open}/?{f}\ {content}",
                $@"{open}/?{i}\ {content}",
                $@"{open}{e}\ {i}\ {content}",
                $@"{open}{e}{close}"
            }) + ")";
        }

        /// <summary>
        /// Escapes a token literal into a sequence of <c>\uXXXX</c> escapes so tokens containing
        /// regex metacharacters can be embedded safely in a pattern.
        /// </summary>
        private static string Escape(string token) => string.Join("", token.ToCharArray().Select(ch => $"\\u{(int)ch:X4}"));

        /// <summary>
        /// Cache of <see cref="Regex"/> instances keyed by pattern and options. The relatively expensive pattern
        /// parsing happens once per distinct pattern and is reused across renders. It is bounded to
        /// <see cref="RegexCacheLimit"/> entries so a long-running app that renders many distinct templates cannot
        /// grow it without limit; when the limit is reached the cache is cleared and repopulated on demand.
        /// </summary>
        private static readonly ConcurrentDictionary<(string Pattern, RegexOptions Options), Regex> _regexCache = new ConcurrentDictionary<(string Pattern, RegexOptions Options), Regex>();

        private const int RegexCacheLimit = 1024;

        /// <summary>
        /// Returns a cached <see cref="Regex"/> for the pattern/options, building it on first use.
        /// </summary>
        private static Regex GetRegex(string pattern, RegexOptions options = RegexOptions.None)
        {
            if (_regexCache.Count >= RegexCacheLimit) _regexCache.Clear();
            return _regexCache.GetOrAdd((pattern, options), key => new Regex(key.Pattern, key.Options));
        }

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
            if (original.IndexOf($"{cfg.OpenToken}/{cfg.IfToken} {key}{cfg.CloseToken}", StringComparison.Ordinal) < 0)
                return original;

            var ifLead = Escape($"{cfg.OpenToken}{cfg.IfToken} ");
            var escKey = Escape(key);
            var closeTag = Escape($"{cfg.OpenToken}/{cfg.IfToken} {key}{cfg.CloseToken}");
            var closeTok = Escape(cfg.CloseToken);
            // Optional leading '!' negates the condition; the boundary stops "Age" from also matching "AgeGroup".
            var boundary = $@"(?![\p{{L}}\p{{Nd}}_.])";
            var openCap = $@"{ifLead}(?<neg>!\s*)?{escKey}{boundary}(?<cond>(?:(?!{closeTok}).)*){closeTok}";
            var openNc = $@"{ifLead}(?:!\s*)?{escKey}{boundary}(?:(?!{closeTok}).)*{closeTok}";

            var rx = GetRegex(
                $@"{openCap}(?<inner>(?>{openNc}(?<LEVEL>)|{closeTag}(?<-LEVEL>)|(?!{openNc}|{closeTag}).)+(?(LEVEL)(?!))){closeTag}",
                RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);

            return rx.Matches(original).Cast<Match>().Aggregate(original, (acc, match) =>
            {
                var outcome = EvaluateCondition(value, match.Groups["cond"].Value.Trim(), replacements);
                if (!outcome.HasValue) return acc; // undecidable (boolean form on a non-bool, or unparseable) -> leave verbatim
                var matched = match.Groups["neg"].Success ? !outcome.Value : outcome.Value;

                SplitConditionalSegments(match.Groups["inner"].Value, cfg, out var firstContent, out var elseIfs, out var elseContent);

                string chosen;
                if (matched)
                    chosen = firstContent;
                else
                {
                    chosen = null;
                    foreach (var branch in elseIfs)
                        if (EvaluateExpression(branch.Expr, replacements) == true) { chosen = branch.Content; break; }
                    if (chosen == null) chosen = elseContent;
                }

                return acc.Replace(match.Captures[0].Value, ReplaceConditionals(chosen ?? string.Empty, key, value, cfg, replacements));
            });
        }

        /// <summary>
        /// Splits an <c>{if}</c> block's inner content at its own (depth-0) <c>{else if ...}</c> and <c>{else}</c>
        /// markers, skipping any that belong to a nested <c>{if}</c> block. Returns the content before the first
        /// marker, the ordered else-if branches (each an expression and its content), and the else content (or null).
        /// </summary>
        private static void SplitConditionalSegments(string inner, StringTemplateConfiguration cfg,
            out string firstContent, out List<(string Expr, string Content)> elseIfs, out string elseContent)
        {
            firstContent = inner;
            elseIfs = new List<(string Expr, string Content)>();
            elseContent = null;

            var closeTok = Escape(cfg.CloseToken);
            var rx = GetRegex(
                $@"(?<ifc>{Escape($"{cfg.OpenToken}/{cfg.IfToken} ")})" +
                $@"|(?<ifo>{Escape($"{cfg.OpenToken}{cfg.IfToken} ")})" +
                $@"|(?<elif>{Escape($"{cfg.OpenToken}{cfg.ElseToken} {cfg.IfToken} ")}(?<expr>(?:(?!{closeTok}).)*){closeTok})" +
                $@"|(?<els>{Escape($"{cfg.OpenToken}{cfg.ElseToken}{cfg.CloseToken}")})");

            var boundaries = new List<(int Index, int Length, bool IsElse, string Expr)>();
            var depth = 0;
            foreach (Match m in rx.Matches(inner))
            {
                if (m.Groups["ifo"].Success) depth++;
                else if (m.Groups["ifc"].Success) { if (depth > 0) depth--; }
                else if (depth == 0)
                {
                    if (m.Groups["elif"].Success) boundaries.Add((m.Index, m.Length, false, m.Groups["expr"].Value));
                    else boundaries.Add((m.Index, m.Length, true, null));
                }
            }

            if (boundaries.Count == 0) return;

            firstContent = inner.Substring(0, boundaries[0].Index);
            for (var i = 0; i < boundaries.Count; i++)
            {
                var b = boundaries[i];
                var start = b.Index + b.Length;
                var end = i + 1 < boundaries.Count ? boundaries[i + 1].Index : inner.Length;
                var content = inner.Substring(start, end - start);
                if (b.IsElse) elseContent = content;
                else elseIfs.Add((b.Expr, content));
            }
        }

        /// <summary>
        /// Evaluates an <c>{else if ...}</c> expression: an optional leading <c>!</c> negates it, the key is looked up
        /// in <paramref name="replacements"/>, and the remainder is the condition. Returns <c>null</c> when undecidable.
        /// </summary>
        private static bool? EvaluateExpression(string expr, Dictionary<string, object> replacements)
        {
            expr = expr.Trim();
            var neg = false;
            if (expr.StartsWith("!", StringComparison.Ordinal)) { neg = true; expr = expr.Substring(1).TrimStart(); }

            var keyMatch = GetRegex(@"^[\p{L}\p{Nd}_.]+").Match(expr);
            if (!keyMatch.Success) return null;

            var v = replacements != null && replacements.TryGetValue(keyMatch.Value, out var val) ? val : null;
            var outcome = EvaluateCondition(v, expr.Substring(keyMatch.Length).Trim(), replacements);
            if (!outcome.HasValue) return null;
            return neg ? !outcome.Value : outcome.Value;
        }

        /// <summary>
        /// Resolves any remaining <c>{Key ?? fallback}</c> tokens (whose key was not present in scope) to the fallback.
        /// </summary>
        private static string ResolveMissingCoalesce(string text, StringTemplateConfiguration cfg)
        {
            if (text.IndexOf("??", StringComparison.Ordinal) < 0) return text;
            var pattern = $@"{Regex.Escape(cfg.OpenToken)}[\p{{L}}\p{{Nd}}_.]+\s*\?\?\s*(?<fallback>(?:(?!{Regex.Escape(cfg.CloseToken)}).)+){Regex.Escape(cfg.CloseToken)}";
            return GetRegex(pattern).Matches(text).Cast<Match>().Aggregate(text, (s, m) =>
                s.Replace(m.Value, Unquote(m.Groups["fallback"].Value)));
        }

        /// <summary>
        /// Strips a single pair of surrounding double quotes from a fallback literal, if present.
        /// </summary>
        private static string Unquote(string s)
        {
            s = s.Trim();
            return s.Length >= 2 && s[0] == '"' && s[s.Length - 1] == '"' ? s.Substring(1, s.Length - 2) : s;
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
