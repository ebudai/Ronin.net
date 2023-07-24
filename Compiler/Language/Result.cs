using Ronin.Grammar;

namespace Ronin.Language;

/*internal class Result : Semantic
{
    public Result(Value value, Context context) : base(value) => this.value = value switch
    {
        Inline literal => literal,
        Grammar.Delegate @delegate => new Lambda(@delegate, context),
        Lookup lookup => new Associations(lookup, context),
        Inputs inputs => new Results(inputs, context),
        List list => new Results(list, context),
        Ordinal ordinal => new Results(ordinal, context),
        Reference reference => new UnresolvedDatum(reference, context),
        _ => Fail(value)
    };

    public static implicit operator Inline(Result result) => result.value as Inline;
    public static implicit operator Lambda(Result result) => result.value as Lambda;
    public static implicit operator Associations(Result result) => result.value as Associations;
    public static implicit operator Results(Result result) => result.value as Results;
    public static implicit operator UnresolvedDatum(Result result) => result.value as UnresolvedDatum;

    private Semantic Fail(Statement statement)
    {
        Errors.AddRange(Error.UnhandledSubclass<Value>(statement));
        return null;
    }

    public override bool Equals(object obj)
    {
        if (obj is not Result result) return false;
        return value switch
        {
            Inline literal => result.value is Inline other && literal.Source.Equals(other.Source),
            Lambda lambda => result.value is Lambda other && lambda.Source == other.Source,
            Associations associations => result.value is Associations other && associations.Source == other.Source,
            Results results => result.value is Results other && results.Source == other.Source,
            UnresolvedDatum datum => result.value is UnresolvedDatum other && datum.Source == other.Source,
            _ => false
        };
    }

    public override int GetHashCode() => value.GetHashCode();

    private readonly object value;
}

internal class NamedResult : Result
{
    public Datum Datum { get; }

    public NamedResult(Reference reference, Value value, Context context) : base(value, context)
    {
        Datum = new UnresolvedDatum(reference, context);
    }
}

internal class Results : Semantic
{
    public List<Result> Values { get; } = new();

    public Results(Inputs inputs, Context context) : base(inputs)
    {
        foreach (var input in inputs.Values)
        {
            Value value = input;
            if (value is not null)
            {
                Values.Add(new Result(value, context));
                continue;
            }

            Assignment assignment = input;
            if (assignment is not null)
            {
                Values.Add(new NamedResult(assignment.Reference, assignment.Value, context));
                continue;
            }

            Errors.Add(new DeveloperMistakeUnhandledSubclass<Inputs.Input> { Statement = inputs });
        }
    }

    public Results(List list, Context context) : base(list)
    {
        foreach (var value in list.Values) Values.Add(new Result(value, context));
    }

    public Results(Ordinal ordinal, Context context) : base(ordinal)
    {
        foreach (var value in ordinal.Values) Values.Add(new Result(value, context));
    }
}*/