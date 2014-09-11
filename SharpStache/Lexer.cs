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

                Token? text = null;

                if (offset != brace)
                {
                    text = new Token
                    {
                        Offset = offset,
                        Length = brace - offset,
                        Type = TagType.Text
                    };
                }

                var token = new Token();

                index = SkipWhitespace(template, index);

                if (index == template.Length)
                {
                    if (text != null)
                        yield return (Token)text;
                    yield break;
                }

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
                            token.Type = TagType.Partial;
                            index = SkipWhitespace(template, index + 1);
                            break;
                        default:
                            token.Type = TagType.Attribute;
                            break;
                    }

                    var start = index;
                    var end = ScanWord(template, start);

                    token.Offset = start;
                    token.Length = end - start;

                    ScanBrace(template, close, ref index);

                    if (text != null)
                        yield return (Token)text;
                    yield return token;
                }
                else
                {
                    ScanBrace(template, close, ref index);
                    Standalone(template, ref text, ref index);

                    if (text != null)
                        yield return (Token)text;
                }
            }
        }

        private static void Standalone(string template, ref Token? text, ref int index)
        {
            if (text == null) return;
            var tok = (Token)text;

            int start = tok.Offset + tok.Length - 1;
            int end = index;

            while (start >= tok.Offset)
            {
                if (template[start] == '\r' || template[start] == '\n')
                    break;
                if (!char.IsWhiteSpace(template[start]))
                    return;
                start--;
            }

            while (end < template.Length)
            {
                if (template[end] != '\r' || template[end] != '\n')
                {
                    end++;
                    if (end < template.Length && template[end] == '\n')
                    {
                        end++;
                    }
                    break;
                }
                if (!char.IsWhiteSpace(template[end]))
                {
                    return;
                }
                end++;
            }
            
            tok.Length = start - tok.Offset + 1;
            text = tok.Length > 0 ? (Token?)tok : null;
            index = end;
        }
    }
}
