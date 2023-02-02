// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Grammar.Aggregates;
using Ronin.Grammar.Errors;
using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar;

/// <summary>
///     Restricts a <see cref="Datum"/> or the <see cref="Temporary"/> 
///     resulting from evaluation of a <see cref="Function"/> to a specific type of data
/// </summary>
/// 
/// <example>
///     datatype Car { var speed => number; var price => money; }
/// </example>
internal class Datatype : Syntax, Compiler.IParsable<Datatype>
{
    public Modifiers Is { get; init; }
    public Identifier Identifier { get; init; }
    public Reference Reference { get; init; }
    public Scope Body { get; init; }

    public static Datatype Parse(ref Parser context)
    {
        Parser parser = context;

        var modifiers = Modifiers.Parse(ref parser);

        if (parser.CurrentToken is not Lexicon.Keywords.Datatype) return null;
        parser.Advance();

        if (Identifier.Parse(ref parser) is not Identifier identifier) return null;

        Reference reference = null;
        if (parser.CurrentToken is Assign)
        {
            parser.Advance();
            reference = Reference.Parse(ref parser);
        }

        var body = Scope.Parse(ref parser);
        if (body is null && reference is null) throw new ExpectedSyntaxError<OpenBrace, Assign>(ref context);

        return new Datatype
        {
            Is = modifiers,
            Identifier = identifier,
            Reference = reference,
            Body = body,
            Source = parser.Commit(ref context)
        };
    }
}
