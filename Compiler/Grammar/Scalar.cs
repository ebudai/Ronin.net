using Ronin.Compiler;
using Ronin.Lexicon;

namespace Ronin.Grammar;

internal class Scalar : Syntax, IParsable
{
    public List<Literal> Literals { get; private init; }

    public static Syntax Parse(ref Parser context)
    {
        Parser parser = context;
        List<Literal> values = new();

        while (parser.IsNotFinished)
        {            
            if (parser.Current is not Literal literal) break;
            values.Add(literal);
            parser.Advance();
        }

        if (values.Count is 0) return null;

        return new Scalar { Literals = values, Source = parser.Commit(ref context) };
    }

    public override string ToString() => string.Join(" ", Literals);
}