using Ronin.Compiler;
using Ronin.Lexicon;

namespace Ronin.Grammar;

internal class Module : Scope
{
    public static new Module Parse(ref Parser current)
    {
        Parser parser = current;

        Module values = new();

        while (parser.IsNotFinished)
        {
            var syntax = Statement.Parse(ref parser);
            if (syntax is null) break;
            values.Add(syntax);
            parser.TryAdvance<Terminal>();
        }

        current = parser;
        return values;
    }
}
