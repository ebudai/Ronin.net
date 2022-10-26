using Ronin.Compiler;
using Ronin.Lexicon;

namespace Ronin.Grammar;

internal class Scalar : Syntax, IParsable
{
    internal Literal[] Literals { get; private init; }

    public Scalar(Parser parser, int length) : base(parser, length) { }

    public static Syntax Parse(Parser parser)
    {
        int length = 0;
        List<Literal> literals = new();

        for (int max = parser.Length; length != max; ++length)
        {
            if (parser[length] is Whitespace or Comment) continue;
            if (parser[length] is not Literal literal) break;
            literals.Add(literal);
        }

        return literals.Count is 0 ? null : new Scalar(parser, length) { Literals = literals.ToArray() };
    }
}