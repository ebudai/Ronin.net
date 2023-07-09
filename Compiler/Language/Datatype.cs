using Ronin.Grammar;

namespace Ronin.Language;

internal class Datatype : Semantic
{
    public bool IsOptional { get; set; }

    public Algebra Algebra { get; init; }
    public Context Definition { get; init; }

    public Datatype() { }    

    public Datatype(DatatypeDeclaration declaration, Context context)
    {
        Algebra = new UnresolvedAlgebra
        {
            Reference = declaration.Algebra,
            Context = context
        };
        Definition = context.Define(declaration.Definition);
    }

    static Datatype()
    {

        /*Context fundamental = new() { Parent = Context.Global };
        Words me = new() { Source = new[] { new Word("me") } };

        Fundamental<char> character = new("character") { Definition = fundamental };
        
        Function charAddAssign = new() { Returns = character, Definition = null };

        Parameters characterParameter = new() { Values = new() { } };
        Fundamental<string> text = new("text");
        Fundamental<float> number = new("number");
        Fundamental<long> whole = new("whole number");
        Fundamental<Int128> date = new("date");
        Fundamental<TimeOnly> time = new("time");
        Fundamental<decimal> money = new("money");
        Fundamental<Uri> url = new("url");
        Fundamental<ulong> bits = new("bits");
        Fundamental<bool> maybe = new("maybe");*/
    }
}

internal class Algebra
{
    public List<Datatype> Bases { get; } = new();
    public List<Datatype> Unions { get; } = new();
}

internal class UnresolvedDatatype : Datatype
{
    public Reference Reference { get; init; }
    public Context Context { get; init; }

    public UnresolvedDatatype(Reference reference, Context context)
    {
        Reference = reference;
        Context = context;
    }
}

internal class UnresolvedAlgebra : Algebra
{
    public Reference Reference { get; init; }
    public Context Context { get; init; }
}

internal class Fundamental<T> : Datatype
{
    public Type Type { get; } = typeof(T);

    public Fundamental(string name)
    {
        base.Definition = Definition;
        Identifier identifier = new(name);
        var errors = Context.Global.Add(identifier, this, null);
        Errors.AddRange(errors);
    }

    private static readonly new Context Definition = new() { Parent = Context.Global };
}