using Ronin.Grammar;

namespace Ronin.Language;

internal class Parameter : Semantic
{
    public Words Name { get; init; }
    public Datum Datum { get; init; }

    public Parameter(DatumDeclaration datum, Context context) : base(datum)
    {
        Name name = new(datum.Name);
        foreach (var component in name.Components)
        {
            Words words = component;
            if (words is null)
            {
                Errors.Add(new DataNameCannotIncludeParameters { Statement = datum });
                continue;
            }

            Name = words;
            Datum = new(datum, context);
        }
    }
}