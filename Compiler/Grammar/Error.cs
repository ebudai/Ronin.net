using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar;

internal abstract class Error : Exception
{
    public int Cursor { get; init; }

    public Error(ref Parser parser)
    {
        do
        {
            parser.Advance();
        }
        while (parser.Current is not Sentinel and not Terminal and not Separator and not Close);

        Cursor = parser.Index;
    }
}
