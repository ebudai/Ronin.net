using Ronin.Compiler;
using Ronin.Lexicon;

namespace Ronin.Grammar;

internal class Assignment : Statement, IParsableSyntax<Assignment>
{
    public Comparison Comparison { get; init; }

    public static new Assignment Parse(ref Parser current)
    {
        Parser parser = current;

        if (Comparison.Parse(ref parser) is not Comparison comparison) return null;

        return new Assignment
        {
            Comparison = comparison,
            Source = parser.Commit(ref current)
        };
    }
}
