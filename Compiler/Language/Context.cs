using Ronin.Grammar;

namespace Ronin.Language;

internal class Context
{
    public void Add(Module import) => imports.Add(import);
    public void Add(Datatype datatype) => datatypes.Add(datatype);
    public void Add(Function function) => functions.Add(function);
    public void Add(Datum datum) => data.Add(datum);

    public List<Semantics> Find(Reference reference)
    {
        List<Semantics> found = new();

        foreach (var module in imports)
        {
            /*foreach (var part in module.Parts)
            {
                found.AddRange(part.Context.Find(reference));
            }*/
        }

        foreach (var datatype in datatypes)
        {
            
        }

        return found;
    }

    private readonly List<Module> imports = new();
    private readonly List<Datatype> datatypes = new();
    private readonly List<Function> functions = new();
    private readonly List<Datum> data = new();
}