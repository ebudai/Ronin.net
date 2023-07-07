using Ronin.Grammar.Compound;

namespace Ronin.Language;

internal class Association
{
    public Result From { get; init; }
    public Result To { get; init; }
}

internal class Associations : Semantic
{
    public List<Association> Values { get; } = new();

    public Associations(Lookup lookup, Context context) : base(lookup)
    {
        foreach (var association in lookup.Values)
        {
            Values.Add(new Association
            {
                From = new Result(association.Key, context),
                To = new Result(association.Value, context)
            });
        }
    }
}