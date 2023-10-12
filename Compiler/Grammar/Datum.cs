// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Grammar;

/// <summary>
///     A singular item of data residing in memory
/// </summary>
/// 
/// <example>
///     datatype Building
///     {
///         var floors => number;
///         ↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑
///     }
///     
///     function do stuff(x => number, y => date) { }
///                       ↑↑↑↑↑↑↑↑↑↑↑  ↑↑↑↑↑↑↑↑↑
/// </example>
internal class Datum : Member
{
    public Mutability Mutability { get; init; }
    public Type Type { get; set; }
    public Value Initializer { get; init; }

    public static new Datum Parse(ref Parser current)
    {
        Parser parser = current;

        var mutability = parser.Token as Mutability;
        if (mutability is not null) parser.Advance();

        if (Name.Parse(ref parser) is not Name name)
        {
            return mutability is null ? null : new ExpectedIdentifierError(ref parser);
        }

        Modifiers modifiers = null;
        Type type = null;
        if (parser.TryAdvance<Returns>())
        {
            modifiers = Modifiers.Parse(ref parser);
            type = Type.Unresolved.Parse(ref parser);
        }

        Value initializer = null;
        if (parser.TryAdvance<Assign>())
        {
            initializer = Value.Parse(ref parser);
        }

        if (type is null)
        {
            if (mutability is null || initializer is null) return null;
        }

        current = parser;
        return new Datum
        {
            Mutability = mutability,
            Identifier = new() { name },
            Modifiers = modifiers ?? new(),
            Type = type,
            Initializer = initializer
        };
    }

    public override void ResolveTypes(Scope context)
    {
        base.ResolveTypes(context);
        if (Type is Type.Unresolved unresolved)
        {
            var member = context.Find(unresolved.Reference);
            Type = member as Type ?? new Type.Calculated { Member = member };
        }
        Initializer.ResolveTypes(context);
    }

    public override void ResolveCalculatedTypes(Scope context, List<Statement> calculations, Stack<Statement> circularityCheck)
    {
        if (Type is not Type.Calculated type) return;

        if (circularityCheck.Contains(this))
        {
            Type = new Type.Calculated.CircularityError { Statements = circularityCheck };
            return;
        }

        circularityCheck.Push(this);

        
        //todo Find() the member, create a compiled statement setting the type to the result of the member (function exec or datum value)
    }

    public override void ResolveFunctions(Scope context)
    {
        base.ResolveFunctions(context);
        Type.ResolveFunctions(context);
        Initializer.ResolveFunctions(context);
    }

    public override void ResolveData(Scope context)
    {
        base.ResolveData(context);
        Type.ResolveData(context);
        Initializer.ResolveData(context);
    }

    public new class Unresolved : Datum
    {
        public Reference Reference { get; set; }

        public static new Datum Parse(ref Parser parser) => Reference.Parse(ref parser) is Reference reference ? new Unresolved { Reference = reference } : null;
    }

    public class Calculated : Datum
    {
        public Member Member { get; init; }
    }

    public class ExpectedIdentifierError : Datum, IError
    {
        public ExpectedIdentifierError(ref Parser parser) => Tokens = Unknown.Parse(ref parser).Tokens;

        public string Reason { get; } = "expected identifier";
        public System.ReadOnlyMemory<Token> Tokens { get; init; }
    }

    
}
