using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar;

internal class Reference : RepeatingSyntax<Value>, IParsable
{
    public static Syntax Parse(ref Parser context)
    {
        Parser parser = context;
        List<Value> values = new();

        while (parser.IsNotEmpty)
        {
            ref readonly var token = ref parser[0];

            if (token is Terminal or Separator or Close or Assign or Returns) break;

            if (token is Trivium)
            {
                ++parser.Cursor;
                continue;
            }
            
            var syntax = Value.Parse(ref parser);
            if (syntax is Error or null) return syntax;
            values.Add(Value.FromSyntax(syntax));            
        }

        return values.Count is 0 ? null : new Reference { Values = values.ToArray(), Tokens = parser.GetTokens(ref context) };
    }
}