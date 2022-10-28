using Ronin.Compiler;
using Ronin.Lexicon;

namespace Ronin.Grammar;

internal class Scalar : Syntax, IParsable
{
    internal Literal[] Literals { get; private init; }

    public static Syntax Parse(Parser parser)
    {
        var literals = _literals.Value;
        literals.Clear();

        parser.Cursor = -1;
        while (parser.IsNotEmpty)
        {
            ++parser.Cursor;
            if (parser[0] is Whitespace or Comment) continue;
            if (parser[0] is not Literal literal) break;
            literals.Add(literal);            
        }

        return literals.Count is 0 ? null : new Scalar { Literals = literals.ToArray(), Tokens = parser.Tokens };
    }

    private static readonly ThreadLocal<List<Literal>> _literals = new(() => new());
}