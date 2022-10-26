using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar;

internal class Error : Syntax, IParsable
{
    internal List<Type> Expected { get; init; }

    private Error(Parser parser, int length) : base(parser, length) { }

    public static Error ExpectedTerminal(Parser parser) => new(parser, 1) { Expected = new() { typeof(Terminal) } };

    public static Syntax Parse(Parser parser)
    {
        int length = 0;
        while (length < parser.Length)
        {
            var lexeme = parser[length];
            if (lexeme is Symbol symbol && !symbol.CanBeUsedInNames) break;
            ++length;
        }
        return new Error(parser, length + 1);
    }
}
