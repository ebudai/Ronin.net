using Ronin.Compiler;
using Ronin.Grammar.Declaration;
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
        if (syntax is null) return Error.Parse(ref context);

        if (parser.Current is not Semicolon) return Error.Parse(ref context);        
        
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

    public Datum Datum
    {
        get => _storage as Datum;
        set => _storage = value;
    }

    public Function Function
    {
        get => _storage as Function;
        set => _storage = value;
    }

    public Datatype Datatype
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
    private Statement(Datum datum) => Datum = datum;
    private Statement(Function function) => Function = function;
    private Statement(Datatype datatype) => Datatype = datatype;
    private Statement(Reference reference) => Reference = reference;

    public static implicit operator Statement(PartOf partOf) => new(partOf);
    public static implicit operator Statement(Import import) => new(import);
    public static implicit operator Statement(Datum datum) => new(datum);
    public static implicit operator Statement(Function function) => new(function);
    public static implicit operator Statement(Reference reference) => new(reference);

    public static implicit operator PartOf(Statement statement) => statement.PartOf;
    public static implicit operator Import(Statement statement) => statement.Import;
    public static implicit operator Datum(Statement statement) => statement.Datum;
    public static implicit operator Function(Statement statement) => statement.Function;
    public static implicit operator Reference(Statement statement) => statement.Reference;

    private object _storage;
}
