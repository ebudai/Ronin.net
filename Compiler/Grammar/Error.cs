using Ronin.Compiler;
using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar;

internal abstract class Error : Syntax
{
    public int Cursor { get; init; }

    protected private static Syntax Parse<T>(ref Parser context) where T : Error, new()
    {
        Parser parser = context;

        while (parser.IsNotFinished)
        {
            parser.Advance();
            if (parser.Current is Semicolon or Comma or Close) break;
        }

        return new T { Source = parser.Commit(ref context), Cursor = context.Index };
    }
}
