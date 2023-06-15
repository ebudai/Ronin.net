using System.Diagnostics.CodeAnalysis;

namespace Ronin.Language;

[ExcludeFromCodeCoverage]
internal class Lambda : Semantic
{
    public List<Datum> Data { get; } = new();
    public Context Definition { get; }

    public Lambda(Grammar.Delegate @delegate, Context context) : base(@delegate.Definition)
    {
        foreach (var datum in @delegate.Data) Data.Add(new Datum(datum, context));
        Definition = new(@delegate.Definition, context, false);
    }
}
