using System.Diagnostics.CodeAnalysis;

namespace Ronin.Grammar;

public class Scope : Syntax
{
    public List<Expression> Expressions { get; } = new();
    public Identifier Name { get; set; }
    public Scope Parent { get; set; }

    public Scope() { }

    public static Scope Global { get; }

    public void Add(Datatype datatype) => Datatypes.Add(datatype.Name, datatype);
    public void Add(Datum datum) => Data.Add(datum.Name, datum);
    public void Add(Function function) => Functions.Add(function.Name, function);
    
    private readonly Dictionary<Identifier, Datatype> Datatypes = new();
    private readonly Dictionary<Identifier, Datum> Data = new();
    private readonly Dictionary<Identifier, Function> Functions = new();

    public class Members<T> where T : Syntax
    {
        public void Add(T member) => members.Add(member);

        public List<T> Find(Identifier identifier)
        {
            List<T> found = new();

            foreach (var member in members)
            {
                
            }

            return found;
        }

        private readonly List<T> members = new();
    }
    [ExcludeFromCodeCoverage]
    public override string ToString() => "{ ... }";
}
