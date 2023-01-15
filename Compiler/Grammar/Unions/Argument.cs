using Ronin.Compiler;

namespace Ronin.Grammar.Unions;

internal class Argument : IParsableUnion<Argument>
{
    public static Syntax Parse(ref Parser parser) => Parameter.Parse(ref parser);

    public static implicit operator Parameter(Argument value) => value._storage;

    public static implicit operator Argument(Syntax syntax) => syntax switch
    {
        Parameter parameter => new() { _storage = parameter },
        _ => null,
    };

    private Parameter _storage;
}