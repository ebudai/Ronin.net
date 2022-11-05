using Ronin.Compiler;
using Ronin.Lexicon;

namespace Ronin.Grammar;

internal class Scalar : RepeatingSyntax<Literal>, IParsable
{
    public static Syntax Parse(ref Parser context)
    {
        Parser parser = context;
        List<Literal> values = new();
        while (parser.IsNotEmpty)
        {
            ref readonly var token = ref parser[0];
            if (token is not Trivium)
            {
                if (token is not Literal literal) break;
                values.Add(literal);
            }
            ++parser.Cursor;
        }

        return values.Count is 0 ? null : new Scalar { Values = values.ToArray(), Tokens = parser.GetTokens(ref context) };
    }
}