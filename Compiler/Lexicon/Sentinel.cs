using Ronin.Compiler;

namespace Ronin.Lexicon;

internal class Sentinel : Token
{
    public static readonly Sentinel Instance = new();

    private Sentinel() : base(new Lexer(string.Empty), 0) { }
}
