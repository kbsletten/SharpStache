using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SharpStache
{
    /// <summary>
    /// A <see cref="http://mustache.github.io">Mustache</see> template renderer for when style matters
    /// </summary>
    public static class SharpStache
    {
        #region Templates

        public interface ITemplate
        {
            void Render(StringBuilder builder, object value);
        }

        public class LiteralTemplate : ITemplate
        {
            public readonly string Text;

            public LiteralTemplate(string text)
            {
                Text = text;
            }

            public void Render(StringBuilder builder, object value)
            {
                builder.Append(Text);
            }
        }

        public class LoopTemplate : AttrTemplate
        {
            public readonly ITemplate[] Inner;

            public LoopTemplate(string name, ITemplate[] inner)
                : base(name)
            {
                Inner = inner;
            }

            public override void Render(StringBuilder builder, object value)
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
                            template.Render(builder, v);

                        }
                    }
                }
                else
                {
                    foreach (var template in Inner)
                    {
                        template.Render(builder, value);
                    }
                }
            }
        }

        public class NotTemplate : AttrTemplate
        {
            public readonly ITemplate[] Inner;

            public NotTemplate(string name, ITemplate[] inner)
                : base(name)
            {
                Inner = inner;
            }

            public override void Render(StringBuilder builder, object value)
            {
                var val = Get(value);

                if ((val != null) &&
                    (!(val is bool) || (bool)val) &&
                    (!(val is IEnumerable) || ((IEnumerable)val).Cast<object>().Any()))
                    return;

                foreach (var template in Inner)
                {
                    template.Render(builder, value);
                }
            }
        }

        public class EscapeTemplate : AttrTemplate
        {
            public EscapeTemplate(string name)
                : base(name)
            {

            }

            public override void Render(StringBuilder buidler, object value)
            {
                buidler.Append(Get(value));
            }
        }

        public class AttrTemplate : ITemplate
        {
            public readonly string Name;

            public AttrTemplate(string name)
            {
                Name = name;
            }

            public object Get(object value)
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

            public virtual void Render(StringBuilder buidler, object value)
            {
                buidler.Append(WebUtility.HtmlEncode((Get(value) ?? "").ToString()));
            }
        }

        private static IEnumerable<ITemplate> GetTemplates(string template, IEnumerator<Token> tokens, string context = null)
        {
            while (tokens.MoveNext())
            {
                var token = tokens.Current;

                var text = template.Substring(token.Offset, token.Length);

                switch (token.Type)
                {
                    case TagType.Text:
                        yield return new LiteralTemplate(text);
                        break;
                    case TagType.Escaped:
                        yield return new EscapeTemplate(text);
                        break;
                    case TagType.Attribute:
                        yield return new AttrTemplate(text);
                        break;
                    case TagType.Loop:
                        yield return new LoopTemplate(text, GetTemplates(template, tokens, text).ToArray());
                        break;
                    case TagType.Not:
                        yield return new NotTemplate(text, GetTemplates(template, tokens, text).ToArray());
                        break;
                    case TagType.Partial:
                        throw new NotImplementedException("Partials not implemented");
                    case TagType.End:
                        if (context != text)
                            throw new Exception("Unmatched end tag. Got " + text + " expecting " + (context ?? "nothing"));
                        yield break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        #endregion

        #region Lexing

        private enum TagType
        {
            Text,
            Attribute,
            Escaped,
            Loop,
            Not,
            Partial,
            End
        }

        private struct Token
        {
            public TagType Type;
            public int Offset;
            public int Length;
        }

        private static int ScanWord(string template, int index)
        {
            while (index < template.Length)
            {
                if (!(template[index] == '.' || char.IsLetter(template[index])))
                    return index;
                index++;
            }
            throw new Exception("Reached end of input while parsing");
        }

        private static int SkipWhitespace(string template, int index)
        {
            while (index < template.Length)
            {
                if (!char.IsWhiteSpace(template[index]))
                    return index;
                index++;
            }
            return index;
        }

        private static int ScanBrace(string template, string brace, ref int index)
        {
            var braceIndex = 0;
            var braceOffset = index;
            while (index < template.Length)
            {
                if (template[index] == brace[braceIndex])
                {
                    braceIndex++;
                }
                else
                {
                    index = braceOffset;
                    braceOffset++;
                    braceIndex = 0;
                }
                index++;

                if (braceIndex == brace.Length)
                    return braceOffset;
            }
            return braceOffset;
        }

        private static IEnumerable<Token> GetTokens(string template, string open = "{{", string close = "}}")
        {
            var index = 0;

            while (index < template.Length)
            {
                var offset = index;
                var brace = ScanBrace(template, open, ref index);

                if (offset != brace)
                {
                    yield return new Token
                    {
                        Offset = offset,
                        Length = brace - offset,
                        Type = TagType.Text
                    };
                }

                var token = new Token();

                index = SkipWhitespace(template, index);

                if (index == template.Length)
                    yield break;

                if (template[index] != '!')
                {
                    switch (template[index])
                    {
                        case '&':
                            token.Type = TagType.Escaped;
                            index = SkipWhitespace(template, index + 1);
                            break;
                        case '#':
                            token.Type = TagType.Loop;
                            index = SkipWhitespace(template, index + 1);
                            break;
                        case '/':
                            token.Type = TagType.End;
                            index = SkipWhitespace(template, index + 1);
                            break;
                        case '^':
                            token.Type = TagType.Not;
                            index = SkipWhitespace(template, index + 1);
                            break;
                        case '>':
                            throw new NotImplementedException("Partials not implemented");
                        default:
                            token.Type = TagType.Attribute;
                            break;
                    }

                    var start = index;
                    var end = ScanWord(template, start);

                    token.Offset = start;
                    token.Length = end - start;

                    ScanBrace(template, close, ref index);

                    var shouldignore =
                        token.Type == TagType.Loop ||
                        token.Type == TagType.Not ||
                        token.Type == TagType.End;

                    if (shouldignore && index < template.Length && template[index] == '\r')
                        index++;
                    if (shouldignore && index < template.Length && template[index] == '\n')
                        index++;

                    yield return token;
                }
                else
                {
                    ScanBrace(template, close, ref index);

                    if (index < template.Length && template[index] == '\r')
                        index++;
                    if (index < template.Length && template[index] == '\n')
                        index++;
                }
            }
        }

        #endregion

        public static string Render(string template, object value)
        {
            var templates = GetTemplates(template, GetTokens(template).GetEnumerator());
            var builder = new StringBuilder();
            foreach (var temp in templates)
            {
                temp.Render(builder, value);
            }
            return builder.ToString();
        }
    }
}
