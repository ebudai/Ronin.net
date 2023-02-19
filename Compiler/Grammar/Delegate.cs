using Ronin.Compiler;
using Ronin.Grammar.Aggregates;
using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar;

internal class Delegate : Syntax, Compiler.IParsable<Delegate>
{
    public List<Datum> Data { get; init; }
    public Scope Body { get; init; }

    public static Delegate Parse(ref Parser context)
    {
        Parser parser = context;

        List<Datum> data;
        var datum = Datum.Parse(ref parser);
        if (datum is null)
        {
            var parameters = Parameters.Parse(ref parser);
            data = parameters?.Values;
            if (data is not null && parser.FailedToConsume<Returns>()) return null;
        }
        else
        {
            data = new List<Datum> { datum };
            if (parser.PreviousToken is not Returns) return null;
        }

        if (Scope.Parse(ref parser) is not Scope body) return null;

        return new Delegate
        {
            Data = data,
            Body = body,
            Source = parser.Commit(ref context)
        };
    }
}
