using OneOf;
using Ronin.Compiler;

namespace Ronin.Grammar;

[GenerateOneOf]
internal partial class Definition : OneOfBase<Scope, Value>, IGrammar<Definition>
{
    public static Definition Parse(ref Parser current)
        => Scope.Parse(ref current) is Scope scope 
            ? scope 
            : Grammar.Value.Parse(ref current);
}