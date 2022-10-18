using Ronin.Compiler;

namespace Ronin.Token.Symbols;

internal class Returns : Symbol
{
    public const string character = "=>";

    public Returns(Lexer lexer) : base(lexer, character.Length) { }

    public static new Returns Lex(Lexer lexer) => !lexer.IsEmpty && lexer.StartsWith(character) ? new Returns(lexer) : null;
}
