// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Grammar.Errors;
using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar;

/// <summary>
///     Named data used for passing information into a <see cref="Function"/>
/// </summary>
/// <example>function withdraw(var amount => money) from (account => Account) { }</example>
/// <exception cref="UnspecifiedDatatypeError"/>
internal class Parameter : Syntax, Compiler.IParsable<Parameter>
{
    public Modifiers Is { get; init; }
    public Name Name { get; init; }
    public Reference Datatype { get; init; }
    public Value Initializer { get; init; }

    public static Parameter Parse(ref Parser context)
    {
        Parser parser = context;

        if (Name.Parse(ref parser) is not Name name) return null;

        Modifiers modifiers = null;
        Reference datatype = null;
        if (parser.Current is Returns)
        {
            parser.Advance();

            modifiers = Modifiers.Parse(ref parser);

            datatype = Reference.Parse(ref parser);
            
            if (datatype is null) throw new UnspecifiedDatatypeError(ref context);
        }
        
        Value initializer = null;
        if (parser.Current is Assign)
        {
            parser.Advance();
            initializer = Value.Parse(ref parser);
        }

        if (datatype is null && initializer is null) throw new UnspecifiedDatatypeError(ref context);

        return new Parameter
        {
            Name = name,
            Is = modifiers,
            Datatype = datatype,
            Initializer = initializer,
            Source = parser.Commit(ref context)
        };
    }
}