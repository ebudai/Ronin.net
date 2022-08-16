using Ronin.Grammar.Modifier;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Grammar;

public class Scope : Modifiable
{
    public List<Expression> Expressions { get; } = new();

    public Scope() { }

    public static Scope Global { get; }
    public Identifier Name { get; set; }

    public void Add(Datatype datatype) => Datatypes.Add(datatype.Name, datatype);
    public void Add(Datum datum) => Data.Add(datum.Name, datum);
    public void Add(Function function) => Functions.Add(function.Name, function);
    public void Add(Scope scope) => Scopes.Add(scope.Name, scope);

    
    private readonly Dictionary<Identifier, Datatype> Datatypes = new();
    private readonly Dictionary<Identifier, Datum> Data = new();
    private readonly Dictionary<Identifier, Function> Functions = new();
    private readonly Dictionary<Identifier, Scope> Scopes = new();    

    [ExcludeFromCodeCoverage]
    public override string ToString() => "{ ... }";

    
}
