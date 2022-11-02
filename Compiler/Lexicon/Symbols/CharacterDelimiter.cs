using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class CharacterDelimiter : Symbol
{
    public const char character = '\'';
    public const string symbol = "'";

    private CharacterDelimiter(Lexer lexer) : base(lexer, symbol.Length) { }

    public static new CharacterDelimiter Lex(Lexer lexer) => lexer.IsNotEmpty && lexer[0] is character ? new CharacterDelimiter(lexer) : null;

    internal override bool CanBeUsedInNames => true;
}
