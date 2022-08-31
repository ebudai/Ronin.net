using Ronin.Compiler;

namespace Ronin.Tokens.Literals;

internal partial class Literal
{
    internal class Text : Token, ILexable<Text>
    {
        internal Text(Lexer lexer, int length) : base(lexer, length) { }

        public static Text Lex(Lexer lexer)
        {
            var span = lexer.Sourcecode.Span;
            if (span.IsEmpty || span[0] is not '"') return null;

            var index = 1;
            var length = span[index..].IndexOf('"');
            if (length is < 0)
            {
                lexer.Error = "unterminated text literal";
                return null;
            }
            while (span[index + length - 1] is '\\')
            {
                index += length + 1;
                length = span[index..].IndexOf('"');
            }
            return new Text(lexer, index + length + 1);
        }
    }
}