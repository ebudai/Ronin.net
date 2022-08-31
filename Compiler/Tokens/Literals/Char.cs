using Ronin.Compiler;

namespace Ronin.Tokens.Literals;

internal partial class Literal
{
    internal class Char : Token, ILexable<Char>
    {
        public Char(Lexer lexer, int length) : base(lexer, length) { }

        public static Char Lex(Lexer lexer)
        {
            var span = lexer.Sourcecode.Span;
            if (span.IsEmpty || span[0] is not '\'') return null;

            var length = span[1..].IndexOf('\'');
            if (length is < 0)
            {
                lexer.Error = "unterminated character literal";
                return null;
            }
            if (length is 0)
            {
                lexer.Error = "empty character literal";
                return null;
            }
            if (length is not 1)
            {
                lexer.Error = "bad unicode literal";
                return null;
            }
            return new Char(lexer, length + 2);
        }
    }

}
