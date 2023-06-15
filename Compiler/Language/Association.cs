using Ronin.Grammar.Compound;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Language;

[ExcludeFromCodeCoverage]
internal class Association
{
    public Result From { get; init; }
    public Result To { get; init; }
}

[ExcludeFromCodeCoverage]
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