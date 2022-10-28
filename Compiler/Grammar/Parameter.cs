using Ronin.Compiler;
using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar;

internal class Parameter : Syntax, IParsable
{
    internal Modifiers Is { get; init; }
    internal string Name { get; init; }    
    internal Value Initializer { get; init; }

    public static Syntax Parse(Parser parser) => ExplicitParameter.Parse(parser) ?? ImplicitParameter.Parse(parser);
}

internal class ExplicitParameter : Parameter, IParsable
{
    internal Reference Datatype { get; init; }

    public static new Syntax Parse(Parser parser)
    {
        Parser attempt = new(parser);

        if (Grammar.Name.Parse(attempt) is not Name name) return null;

        if (attempt[0] is not Returns) return null;

        ++attempt.Cursor;

        var modifiers = Modifiers.Parse(attempt) as Modifiers;

        var datatype = Reference.Parse(attempt);

        if (datatype is Error) return datatype;
        
        Syntax initializer = null;
        if (attempt[0] is Assign)
        {
            ++attempt.Cursor;
            initializer = Value.Parse(attempt);
        }

        parser.Cursor = attempt.Cursor;

        return new ExplicitParameter
        {
            Name = string.Join(' ', name.Names),
            Is = modifiers,
            Datatype = datatype as Reference,
            Initializer = initializer,
            Tokens = parser[..attempt.Cursor],
        };
    }
}

internal class ImplicitParameter : Parameter, IParsable
{
    public static new Syntax Parse(Parser parser)
    {
        Parser attempt = new(parser);

        if (Grammar.Name.Parse(attempt) is not Name name) return null;

        if (attempt[0] is not Returns) return null;

        ++attempt.Cursor;

        var modifiers = Modifiers.Parse(attempt) as Modifiers;

        Syntax initializer = null;
        if (attempt[0] is Assign)
        {
            ++attempt.Cursor;
            initializer = Value.Parse(attempt);
        }

        parser.Cursor = attempt.Cursor;

        return new ImplicitParameter
        {
            Name = string.Join(' ', name.Names),
            Is = modifiers,
            Initializer = initializer,
            Tokens = parser[..attempt.Cursor],
        };
    }
}
