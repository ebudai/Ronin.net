using Ronin.Compiler;
using Ronin.Lexicon;

namespace Ronin.Grammar;

internal class Scalar : RepeatingSyntax<Literal>, IParsable
{
    public static Syntax Parse(Parser parser)
    {
        buffer.Clear();

        parser.Cursor = -1;
        while (parser.IsNotEmpty)
        {
            ++parser.Cursor;
            if (parser[0] is Trivium) continue;
            if (parser[0] is not Literal literal) break;
            buffer.Add(literal);            
        }

        return buffer.Count is 0 ? null : new Scalar { Elements = buffer.ToArray(), Tokens = parser.Tokens };
    }
}