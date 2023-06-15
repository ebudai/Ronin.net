using Ronin.Grammar;

namespace Ronin.Language;

internal class Lambda : Context
{
    public List<Datum> Data { get; } = new();

    public Lambda(Grammar.Delegate @delegate, Context context) : base(@delegate.Definition, context)
    {
        foreach (var datum in @delegate.Data) Data.Add(new Datum(datum, context));
    }
}
