using Ronin.Compiler;
using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar;

internal class Reference : Syntax, IParsable
{
    public static Syntax Parse(ref Parser context)
    {
        Parser parser = context;
        List<Value> values = new();

        while (parser.IsNotFinished)
        {
            if (parser.Current is Semicolon or Comma or Close or Assign or Returns or OpenBrace) break;
            
            var syntax = Value.Parse(ref parser);
            if (syntax is Error or null) return syntax;
            values.Add(Value.FromSyntax(syntax));            
        }

        return values.Count is 0 ? null : new Reference { Values = values.ToArray(), Source = parser.Commit(ref context) };
    }

    internal Value[] Values;
}