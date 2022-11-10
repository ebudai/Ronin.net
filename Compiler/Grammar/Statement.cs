using Ronin.Compiler;
using Ronin.Grammar.Declaration;
using Ronin.Grammar.Errors;
using Ronin.Lexicon;
using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar;

internal class Statement : Syntax, IParsable
{
    public static Syntax Parse(ref Parser context)
    {
        Parser parser = context;

        var syntax = PartOf.Parse(ref parser)
                ?? Import.Parse(ref parser)
                ?? Datum.Parse(ref parser)
                ?? Function.Parse(ref parser)
                ?? Datatype.Parse(ref parser)
                ?? Reference.Parse(ref parser);

        if (syntax is Error error)
        {
            context.Index = error.Cursor;
            return error;
        }
        if (syntax is null) return UnknownSyntax.Parse(ref context);

        if (parser.Current is not Terminal and not Sentinel) return ExpectedTerminal.Parse(ref context);        
        
        context = parser;
        return FromSyntax(syntax);
    }

    public static Statement FromSyntax(Syntax syntax) => syntax switch 
    {
        PartOf partOf => new(partOf),
        Import import => new(import),
        Datum datum => new(datum),
        Function function => new(function),
        Datatype datatype => new(datatype),
        Reference reference => new(reference),
        _ => null,
    };

    public PartOf PartOf
    {
        get => _storage as PartOf;
        set => _storage = value;
    }

    public Import Import
    {
        get => _storage as Import;
        set => _storage = value;
    }

    public Datum DatumDeclaration
    {
        get => _storage as Datum;
        set => _storage = value;
    }

    public Function FunctionDeclaration
    {
        get => _storage as Function;
        set => _storage = value;
    }

    public Datatype DatatypeDeclaration
    {
        get => _storage as Datatype;
        set => _storage = value;
    }

    public Reference Reference
    {
        get => _storage as Reference;
        set => _storage = value;
    }

    private Statement(PartOf partOf) => PartOf = partOf;
    private Statement(Import import) => Import = import;
    private Statement(Datum datum) => DatumDeclaration = datum;
    private Statement(Function function) => FunctionDeclaration = function;
    private Statement(Datatype datatype) => DatatypeDeclaration = datatype;
    private Statement(Reference reference) => Reference = reference;

    private object _storage;
}
