// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Grammar;

internal class Association : Statement, Compiler.IParsable<Association>
{
    public Value Destination { get; set; }
    public Assignment Assignment { get; init; }
    public Value Origin { get; set; }

    public static new Association Parse(ref Parser current)
    {
        Parser parser = current;

        if (Value.Parse(ref parser) is not Value destination) return null;
        if (parser.Token is not Assignment assignment) return null;
        parser.Advance();

        if (Value.Parse(ref parser) is not Value origin) return null;

        current = parser;
        return new Association
        {
            Destination = destination,
            Assignment = assignment,
            Origin = origin
        };
    }

    [ExcludeFromCodeCoverage]
    public override void ResolveReferences(Scope context)
    {
        if (Destination is Member.Unresolved destination)
        {
            Destination = context.Find(destination.Reference);
        }
        else
        {
            Destination.ResolveReferences(context);
        }
        if (Origin is Member.Unresolved origin)
        {
            Origin = context.Find(origin.Reference);
        }
        else
        {
            Origin.ResolveReferences(context);
        }        
    }
}
