using Ronin.Compiler;
using Ronin.Grammar.Aggregates;

namespace Ronin.Grammar.Unions;

internal class IdentifierComponent : IParsableUnion<IdentifierComponent>
{
    public static Syntax Parse(ref Parser context)
        => Name.Parse(ref context)
        ?? Parameters.Parse(ref context);

    public static implicit operator Name(IdentifierComponent component) => component._storage as Name;
    public static implicit operator Parameters(IdentifierComponent component) => component._storage as Parameters;

    public static implicit operator IdentifierComponent(Syntax syntax) => syntax switch
    {
        Name name => new() { _storage = name },
        Parameters parameters => new() { _storage = parameters },
        _ => null,
    };

    /*public bool Matches(Value value)
    {
        Scalar scalar = value;
        if (scalar is not null) return Matches(scalar);
        {
            if (_storage is Parameters parameters)
            {
                //return parameters.Values.Length is 1 && 
            }
        }


        return false;
    }

    private bool Matches(Scalar scalar)
    {
        if (_storage is Parameters parameters)
        {

        }
    }*/

    private object _storage;
}