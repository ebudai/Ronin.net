// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Grammar.Aggregates;
using Ronin.Grammar.Errors;
using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar;

/// <summary>
///     Restricts a <see cref="Datum"/>, <see cref="Parameter"/>, or <see cref="Function"/> return <see cref="Value"/> to a specific type of data
/// </summary>
/// 
/// <example>
///     datatype Car { var speed => number; var price => money; }
/// </example>
internal class Datatype : Syntax, Compiler.IParsable<Datatype>
{
    public Modifiers Is { get; init; }
    public Identifier Identifier { get; init; }
    public List<Algebra> Algebra { get; init; }
    public Scope Body { get; init; }

    public static Datatype Parse(ref Parser context)
    {
        Parser parser = context;

        var modifiers = Modifiers.Parse(ref parser);

        if (parser.Current is not Lexicon.Keywords.Datatype) return null;
        parser.Advance();

        if (Identifier.Parse(ref parser) is not Identifier identifier) return null;

        List<Algebra> algebra = null;
        if (parser.Current is Assign)
        {
            parser.Advance();
            algebra = parser.ParseRepeating<Algebra>();
        }

        if (Scope.Parse(ref parser) is not Scope body) throw new ExpectedSyntaxError<OpenBrace>(ref context);

        return new Datatype
        {
            Is = modifiers,
            Identifier = identifier,
            Algebra = algebra,
            Body = body,
            Source = parser.Commit(ref context)
        };
    }
}
