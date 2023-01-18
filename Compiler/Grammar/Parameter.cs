using Ronin.Compiler;
using Ronin.Grammar.Errors;
using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar;

internal class Parameter : Syntax, IParsable
{
    public Modifiers Is { get; init; }
    public Name Name { get; init; }
    public Reference Datatype { get; init; }
    public Value Initializer { get; init; }

    public static Syntax Parse(ref Parser context)
    {
        Parser parser = context;

        if (Name.Parse(ref parser) is not Name name) return null;

        Modifiers modifiers = null;
        Syntax datatype = null;
        if (parser.Current is Returns)
        {
            parser.Advance();

            modifiers = Modifiers.Parse(ref parser) as Modifiers;

            datatype = Reference.Parse(ref parser);
            if (datatype is Error) return datatype;
            if (datatype is null) return ExpectedReferenceError.Parse(ref context);
        }
        
        Syntax initializer = null;
        if (parser.Current is Assign)
        {
            parser.Advance();
            initializer = Value.Parse(ref parser);
        }

        if (datatype is null && initializer is null) return UnspecifiedDatatypeError.Parse(ref context);

        return new Parameter
        {
            Name = name,
            Is = modifiers,
            Datatype = datatype as Reference,
            Initializer = initializer as Value,
            Source = parser.Commit(ref context)
        };
    }

    /*public override string ToString()
    {
        var code = Is + " " + Name;
        if (Datatype is not null) code += " " + Datatype;
        if (Initializer is not null) code += " = " + Initializer;
        return code;
    }*/

}