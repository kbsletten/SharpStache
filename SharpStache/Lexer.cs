using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpStache
{
    internal enum TagType
    {
        Text,
        Attribute,
        Escaped,
        Loop,
        Not,
        Partial,
        End
    }

    internal struct Token
    {
        public TagType Type;
        public int Offset;
        public int Length;
    }

    internal static class Lexer
    {
        internal static int ScanWord(string template, int index)
        {
            while (index < template.Length)
            {
                if (!(template[index] == '.' || char.IsLetter(template[index])))
                    return index;
                index++;
            }
            throw new Exception("Reached end of input while parsing");
        }

        internal static int SkipWhitespace(string template, int index)
        {
            while (index < template.Length)
            {
                if (!char.IsWhiteSpace(template[index]))
                    return index;
                index++;
            }
            return index;
        }

        internal static int ScanBrace(string template, string brace, ref int index)
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

        internal static IEnumerable<Token> GetTokens(string template, string open = "{{", string close = "}}")
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
    }
}
