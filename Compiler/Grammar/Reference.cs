using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar;

internal class Reference : RepeatingSyntax<Value>, IParsable
{
    public static Syntax Parse(ref Parser context)
    {
        Parser parser = context;
        t_buffer.Clear();

        while (parser.IsNotEmpty)
        {
            if (parser[0] is not Trivium)
            {
                var component = Value.Parse(ref parser);
                if (component is Error or null) return component;
                t_buffer.Add(component as Value);
            }
            
            ++parser.Cursor;

            if (parser[0] is Terminal or Separator or Close) break;
        }

        return t_buffer.Count is 0 ? null : new Reference { Elements = t_buffer.ToArray(), Tokens = parser.GetTokens(ref context) };
    }
}