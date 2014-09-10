using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Linq;

namespace SharpStache
{
    internal interface ITemplate
    {
        void Render(StringBuilder builder, IDictionary<string, string> partials, object value);
    }

    internal static class Parser
    {
        internal class LiteralTemplate : ITemplate
        {
            internal readonly string Template;
            internal readonly int Offset;
            internal readonly int Length;

            internal LiteralTemplate(string template, int offset, int length)
            {
                Template = template;
                Offset = offset;
                Length = length;
            }

            void ITemplate.Render(StringBuilder builder, IDictionary<string, string> partials, object value)
            {
                builder.Append(Template, Offset, Length);
            }
        }

        internal class LoopTemplate : MemberTemplate
        {
            internal readonly ITemplate[] Inner;

            internal LoopTemplate(string name, ITemplate[] inner)
                : base(name)
            {
                Inner = inner;
            }

            internal override void Render(StringBuilder builder, IDictionary<string, string> partials, object value)
            {
                var val = Get(value);

                if ((val == null) ||
                    (val is bool && !(bool)val))
                    return;

                var enumerable = val is string ? null : val as IEnumerable;
                if (enumerable != null)
                {

                    foreach (var v in enumerable)
                    {
                        foreach (var template in Inner)
                        {
                            template.Render(builder, partials, v);

                        }
                    }
                }
                else
                {
                    foreach (var template in Inner)
                    {
                        template.Render(builder, partials, val);
                    }
                }
            }
        }

        internal class NotTemplate : MemberTemplate
        {
            internal readonly ITemplate[] Inner;

            internal NotTemplate(string name, ITemplate[] inner)
                : base(name)
            {
                Inner = inner;
            }

            internal override void Render(StringBuilder builder, IDictionary<string, string> partials, object value)
            {
                var val = Get(value);

                if ((val != null) &&
                    (!(val is bool) || (bool)val) &&
                    (!(val is IEnumerable) || ((IEnumerable)val).Cast<object>().Any()))
                    return;

                foreach (var template in Inner)
                {
                    template.Render(builder, partials, value);
                }
            }
        }

        internal class PartialTemplate : ITemplate
        {
            internal readonly string Name;

            internal PartialTemplate(string name)
            {
                Name = name;
            }

            void ITemplate.Render(StringBuilder builder, IDictionary<string, string> partials, object value)
            {
                string partial;

                if (partials.TryGetValue(Name, out partial))
                {
                    var templates = GetTemplates(partial);
                    foreach (var template in templates)
                    {
                        template.Render(builder, partials, value);
                    }
                }
            }
        }

        internal class EscapeTemplate : MemberTemplate
        {
            internal EscapeTemplate(string name)
                : base(name)
            {

            }

            internal override void Render(StringBuilder buidler, IDictionary<string, string> partials, object value)
            {
                buidler.Append(Get(value));
            }
        }

        internal abstract class MemberTemplate : ITemplate
        {
            internal readonly string Name;

            internal MemberTemplate(string name)
            {
                Name = name;
            }

            internal object Get(object value)
            {
                if (Name == ".")
                    return value;

                if (value == null)
                    return null;

                var dict = value as IDictionary;
                if (dict != null)
                {
                    return dict[Name];
                }

                var field = value.GetType().GetField(Name);
                if (field != null)
                {
                    return field.GetValue(value);
                }

                var prop = value.GetType().GetProperty(Name);
                if (prop != null)
                {
                    return prop.GetValue(value, null);
                }

                var meth = value.GetType().GetMethod(Name);
                if (meth != null)
                {
                    return meth.Invoke(value, null);
                }

                return null;
            }

            void ITemplate.Render(StringBuilder builder, IDictionary<string, string> partials, object value)
            {
                Render(builder, partials, value);
            }

            internal abstract void Render(StringBuilder builder, IDictionary<string, string> partials, object value);
        }

        internal class AttrTemplate : MemberTemplate
        {
            internal AttrTemplate(string name)
                : base(name)
            {
            }

            internal override void Render(StringBuilder buidler, IDictionary<string, string> partials, object value)
            {
                buidler.Append(WebUtility.HtmlEncode((Get(value) ?? "").ToString()));
            }
        }

        internal static IEnumerable<ITemplate> GetTemplates(string template)
        {
            return GetTemplates(template, Lexer.GetTokens(template).GetEnumerator());
        }

        internal static IEnumerable<ITemplate> GetTemplates(string template, IEnumerator<Token> tokens, string context = null)
        {
            while (tokens.MoveNext())
            {
                var token = tokens.Current;

                if (token.Type == TagType.Text)
                {
                    yield return new LiteralTemplate(template, token.Offset, token.Length);
                    continue;
                }

                var text = template.Substring(token.Offset, token.Length);

                switch (token.Type)
                {
                    case TagType.Escaped:
                        yield return new EscapeTemplate(text);
                        break;
                    case TagType.Attribute:
                        yield return new AttrTemplate(text);
                        break;
                    case TagType.Partial:
                        yield return new PartialTemplate(text);
                        break;
                    case TagType.Loop:
                        yield return new LoopTemplate(text, GetTemplates(template, tokens, text).ToArray());
                        break;
                    case TagType.Not:
                        yield return new NotTemplate(text, GetTemplates(template, tokens, text).ToArray());
                        break;
                    case TagType.End:
                        if (context != text)
                            throw new Exception("Unmatched end tag. Got " + text + " expecting " + (context ?? "nothing"));
                        yield break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}