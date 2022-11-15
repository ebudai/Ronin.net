using Ronin.Compiler;
using Ronin.Grammar.Errors;
using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar;

internal class Parameter : Syntax, Compiler.IParsable<Parameter>
{
    internal Modifiers Is { get; protected private init; }
    internal Name Name { get; protected private init; }
    internal Reference Datatype { get; protected private init; }
    internal Value Initializer { get; protected private init; }

    public static Parameter FromSyntax(Syntax syntax) => syntax as Parameter;

    public static Syntax Parse(ref Parser context)
    {
        Parser parser = context;

        var name = Name.Parse(ref parser) as Name;
        if (name is null) return name;

        Modifiers modifiers = null;
        Syntax datatype = null;
        if (parser.Current is Returns)
        {
            parser.Advance();

            modifiers = Modifiers.Parse(ref parser) as Modifiers;

            datatype = Reference.Parse(ref parser);
            if (datatype is Error or null) return datatype ?? ExpectedReference.Parse(ref context);
        }
        
        Syntax initializer = null;
        if (parser.Current is Assign)
        {
            parser.Advance();
            initializer = Value.Parse(ref parser);
        }

        if (datatype is null && initializer is null) return UnknownDatatype.Parse(ref context);

        return new Parameter
        {
            Name = name,
            Is = modifiers,
            Datatype = datatype as Reference,
            Initializer = Value.FromSyntax(initializer),
            Source = parser.Commit(ref context)
        };
    }
}
