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
        void Render(StringBuilder builder, IDictionary<string, string> partials, Stack values);
    }

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

        void ITemplate.Render(StringBuilder builder, IDictionary<string, string> partials, Stack values)
        {
            builder.Append(Template, Offset, Length);
        }
    }

    internal class SectionTemplate : MemberTemplate
    {
        internal readonly ITemplate[] Inner;

        internal SectionTemplate(string name, ITemplate[] inner)
            : base(name)
        {
            Inner = inner;
        }

        internal override void Render(StringBuilder builder, IDictionary<string, string> partials, Stack values)
        {
            var val = Get(values);

            if (val == null)
                return;

            if (val is bool)
            {
                if ((bool)val)
                {
                    foreach (var template in Inner)
                    {
                        template.Render(builder, partials, values);
                    }
                }
                return;
            }

            var enumerable = val is string ? null : val as IEnumerable;
            if (enumerable != null)
            {

                foreach (var v in enumerable)
                {
                    values.Push(v);
                    foreach (var template in Inner)
                    {
                        template.Render(builder, partials, values);

                    }
                    values.Pop();
                }
            }
            else
            {
                values.Push(val);
                foreach (var template in Inner)
                {
                    template.Render(builder, partials, values);
                }
                values.Pop();
            }
        }
    }

    internal class InvertedTemplate : MemberTemplate
    {
        internal readonly ITemplate[] Inner;

        internal InvertedTemplate(string name, ITemplate[] inner)
            : base(name)
        {
            Inner = inner;
        }

        internal override void Render(StringBuilder builder, IDictionary<string, string> partials, Stack values)
        {
            var val = Get(values);

            if ((val != null) &&
                (!(val is bool) || (bool)val) &&
                (!(val is IEnumerable) || ((IEnumerable)val).Cast<object>().Any()))
                return;

            foreach (var template in Inner)
            {
                template.Render(builder, partials, values);
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

        void ITemplate.Render(StringBuilder builder, IDictionary<string, string> partials, Stack values)
        {
            string partial;

            if (partials.TryGetValue(Name, out partial))
            {
                var templates = Parser.GetTemplates(partial);
                foreach (var template in templates)
                {
                    template.Render(builder, partials, values);
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

        internal override void Render(StringBuilder buidler, IDictionary<string, string> partials, Stack values)
        {
            buidler.Append(Get(values));
        }
    }

    internal abstract class MemberTemplate : ITemplate
    {
        internal readonly string[] Name;

        internal MemberTemplate(string name)
        {
            if (name == ".")
            {
                Name = new[] { name };
            }
            else
            {
                Name = name.Split('.');
            }
        }

        internal object Get(Stack values)
        {
            foreach (var value in values)
            {
                var val = GetVal(value, Name.First());
                if (val != null)
                {
                    return Name.Skip(1).Aggregate(val, GetVal);
                }
            }
            return null;
        }

        private object GetVal(object val, string name)
        {
            if (name == ".")
                return val;

            if (val == null)
                return null;

            var dict = val as IDictionary;
            if (dict != null)
            {
                return dict[Name];
            }

            var field = val.GetType().GetField(name);
            if (field != null)
            {
                return field.GetValue(val);
            }

            var prop = val.GetType().GetProperty(name);
            if (prop != null)
            {
                return prop.GetValue(val, null);
            }

            var meth = val.GetType().GetMethod(name);
            if (meth != null)
            {
                return meth.Invoke(val, null);
            }

            return null;
        }

        void ITemplate.Render(StringBuilder builder, IDictionary<string, string> partials, Stack values)
        {
            Render(builder, partials, values);
        }

        internal abstract void Render(StringBuilder builder, IDictionary<string, string> partials, Stack values);
    }

    internal class AttrTemplate : MemberTemplate
    {
        internal AttrTemplate(string name)
            : base(name)
        {
        }

        internal override void Render(StringBuilder buidler, IDictionary<string, string> partials, Stack values)
        {
            buidler.Append(WebUtility.HtmlEncode((Get(values) ?? "").ToString()));
        }
    }

    internal static class Parser
    {
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
                        yield return new SectionTemplate(text, GetTemplates(template, tokens, text).ToArray());
                        break;
                    case TagType.Not:
                        yield return new InvertedTemplate(text, GetTemplates(template, tokens, text).ToArray());
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