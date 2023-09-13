using Ronin.Compiler;
using Ronin.Lexicon;

namespace Ronin.Grammar;

internal class Assignment : Statement, IParsableSyntax<Assignment>
{
    public Set Keyword { get; init; }
    public Comparison Comparison { get; init; }

    public static new Assignment Parse(ref Parser current)
    {
        Parser parser = current;

        if (parser.Token is not Set keyword) return null;
        parser.Advance();

        if (Comparison.Parse(ref parser) is not Comparison comparison) return null;

        return new Assignment
        {
            Keyword = keyword,
            Comparison = comparison,
            Source = parser.Commit(ref current)
        };
    }
}
