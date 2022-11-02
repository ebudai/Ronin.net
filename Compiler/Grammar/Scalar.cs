using Ronin.Compiler;
using Ronin.Lexicon;

namespace Ronin.Grammar;

internal class Scalar : RepeatingSyntax<Literal>, IParsable
{
    public static Syntax Parse(ref Parser context)
    {
        Parser parser = context;
        t_buffer.Clear();
        while (parser.IsNotEmpty)
        {
            ref var token = ref parser[0];
            if (token is not Trivium)
            {
                if (token is not Literal literal) break;
                t_buffer.Add(literal);
            }
            ++parser.Cursor;
        }

        return t_buffer.Count is 0 ? null : new Scalar { Elements = t_buffer.ToArray(), Tokens = parser.GetTokens(ref context) };
    }
}