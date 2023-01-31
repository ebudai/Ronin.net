// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Grammar.Aggregates;

namespace Ronin.Grammar;

/// <summary>
///     Restricts a <see cref="Datum"/>, <see cref="Parameter"/>, or the <see cref="Temporary"/> 
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
    public List<Algebra> Algebra { get; init; } //todo this should just be a reference, to be split up later
    public Body Body { get; init; }

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

        var body = Body.Parse(ref parser);
        if (body is null && algebra is null) throw new Error(ref context);

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
