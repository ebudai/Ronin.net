using Ronin.Compiler;
using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar;

internal class Error : Syntax, IParsable
{
    internal List<Type> Expected { get; init; } = new();
    internal int Cursor { get; init; }

    public static Syntax Parse(ref Parser context)
    {
        Parser parser = context;

        while (parser.IsNotFinished)
        {
            parser.Advance();
            if (parser.Current is Semicolon or Comma or Close) break;
        }

        return new Error { Source = parser.Commit(ref context), Cursor = context.Index };
    }
}
