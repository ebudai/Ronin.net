using Ronin.Compiler;
using Ronin.Grammar.Errors;
using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar;

internal class Parameter : Syntax, IParsable<Parameter>
{
    internal Modifiers Is { get; init; }
    internal string Name { get; init; }
    internal Reference Datatype { get; init; }
    internal Value Initializer { get; init; }

    public static Parameter FromSyntax(Syntax syntax) => syntax as Parameter;

    public static Syntax Parse(ref Parser context)
    {
        Parser parser = context;

        if (Grammar.Name.Parse(ref parser) is not Name name) return null;

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

        if (datatype is null && initializer is null) return ExpectedDatatypeOrInitializer.Parse(ref context);

        return new Parameter
        {
            Name = string.Join(' ', name.Words),
            Is = modifiers,
            Datatype = datatype as Reference,
            Initializer = Value.FromSyntax(initializer),
            Source = parser.Commit(ref context)
        };
    }
}
