using Ronin.Compiler;
using Ronin.Grammar.Aggregates.Scopes;
using Ronin.Grammar.Modifiers;

namespace Ronin.Grammar;

internal class Datum : Syntax, IParsable<Datum>, IIdentifiable
{
    public Identifier Name { get; set; }
    internal Datatype Datatype { get; set; }
    internal string ConstantValue { get; set; }
    internal Accessibility Accessibility { get; set; }
    internal Mutability Mutability { get; set; } 

    public static Datum Parse(ReadOnlyMemory<char> sourcecode)
    {
        throw new NotImplementedException();
    }

    public static string Write(Datum syntax)
    {
        string sourcecode = string.Empty;
        
        // only write 'var' if it is the only non-default modifier
        if (syntax.Accessibility == default && syntax.Mutability == default)
        {
            sourcecode += Enum.GetName(Mutability.var) + ' ';
        }

        //sourcecode += Name;
        return sourcecode;
    }

    private static string[] GetAllModifiers() => typeof(Datum).Assembly.GetTypes()
        .Where(type => type.IsEnum)
        .Where(type => type.Namespace == typeof(Datum).Namespace + nameof(Modifiers))        
        .SelectMany(Enum.GetNames)
        .ToArray();
}