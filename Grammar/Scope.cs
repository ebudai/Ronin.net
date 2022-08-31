namespace Ronin.Grammar;

public class Scope : Syntax, IIdentifiable
{
    public List<Expression> Expressions { get; } = new();
    public Identifier Name { get; set; }
    public Scope Parent { get; set; }

    public Members<Datatype> Datatypes { get; } = new();
    public Members<Datum> Data { get; } = new();
    public Members<Function> Functions { get; } = new();

    public List<Scope> Imported { get; } = new();

    public static Scope Global { get; }

    public Scope() { }

    public void ResolveIdentifiers()
    {
        // resolve scope name and imports first
        for (var i = 0; i < Expressions.Count; ++i)
        {
            var expression = Expressions[i];
            if (expression.Syntax.Count is 0) continue;
            if (expression.Syntax[0] is Identifier identifier)
            {
                if (identifier.Names.Count is 0) continue;
                if (identifier.Names[0].Trim() is "part of")
                {
                    identifier.Names.Remove(0); // remove "part of "
                    Name = identifier;          // set name of this scope to the identifier specified by "part of"
                    Expressions.RemoveAt(i--);  // we only want declarations and function calls in |Expressions| after this function is done
                }
                else if (identifier.Names[0].Trim() is "import")
                {
                    identifier.Names.Remove(0); // remove "import "

                }
            }
        }
    }

    public class Members<T> where T : IIdentifiable
    {
        public void Add(T member) => members.Add(member);

        public List<T> Find(Identifier identifier)
        {
            List<T> found = new();

            foreach (var member in members)
            {
                var matches = identifier.Match(member.Name);

            }

            return found;
        }

        private readonly List<T> members = new();
    }
}
