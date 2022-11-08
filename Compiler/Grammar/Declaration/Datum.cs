using Ronin.Compiler;
using Ronin.Lexicon.Reserved;

namespace Ronin.Grammar.Declaration;

internal class Datum : Syntax, IParsable
{
    internal Declarator Mutability { get; private init; }
    internal string Name { get; private init; }
    internal Modifiers Modifiers { get; private init; }
    internal Reference Datatype { get; private init; }
    internal Value Initializer { get; private init; }

    public static Syntax Parse(ref Parser context)
    {
        Declarator? declarator = context.Current switch
        {
            Variable => Declarator.Variable,
            Constant => Declarator.Constant,
            Reactive => Declarator.Reactive,
            _ => null
        };
        if (declarator is null) return null;

        Parser parser = context;
        parser.Advance();
        var syntax = Parameter.Parse(ref parser);
        if (syntax is Error) return syntax;
        var parameter = syntax as Parameter;

        return new Datum
        {
            Mutability = declarator.Value,
            Name = parameter.Name,
            Modifiers = parameter.Is,
            Datatype = parameter.Datatype,
            Initializer = parameter.Initializer,
            Source = parser.Commit(ref context)
        };
    }

    internal enum Declarator { Variable, Constant, Reactive };
}