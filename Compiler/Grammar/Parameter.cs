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
        if (Grammar.Name.Parse(parser) is not Name name) return null;

        if (parser[0] is not Returns) return null;

        ++parser.Cursor;

        var modifiers = Modifiers.Parse(parser) as Modifiers;

        var datatype = Reference.Parse(parser);
        if (datatype is Error or null) return datatype;
        
        Syntax initializer = null;
        if (parser[0] is Assign)
        {
            ++parser.Cursor;
            initializer = Value.Parse(parser);
        }

        return new ExplicitParameter
        {
            Name = string.Join(' ', name.Names),
            Is = modifiers,
            Datatype = datatype as Reference,
            Initializer = initializer,
            Tokens = parser.Tokens,
        };
    }
}

internal class ImplicitParameter : Parameter, IParsable
{
    public static new Syntax Parse(Parser parser)
    {
        if (Grammar.Name.Parse(parser) is not Name name) return null;

        if (parser[0] is not Returns) return null;

        ++parser.Cursor;

        var modifiers = Modifiers.Parse(parser) as Modifiers;

        Syntax initializer = null;
        if (parser[0] is Assign)
        {
            ++parser.Cursor;
            initializer = Value.Parse(parser);
        }

        return new ImplicitParameter
        {
            Name = string.Join(' ', name.Names),
            Is = modifiers,
            Initializer = initializer,
            Tokens = parser.Tokens,
        };
    }
}
