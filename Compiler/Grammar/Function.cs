// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Grammar;
/// <summary>
///     Ordered grouping of instructions to execute when called
/// </summary>
/// 
/// <example>
///     function florb (x => number) things with (fast => maybe, fun => maybe) stuff => number 
///     {
///         return 8; 
///     }
/// </example>
internal class Function : Member
{
    public Lexicon.Function Keyword { get; init; }
    public Type Returns { get; set; }
    public Scope Definition { get; init; }

    public new static Function Parse(ref Parser current)
    {
        Parser parser = current;

        if (parser.Token is not Lexicon.Function keyword) return null;
        parser.Advance();

        if (Identifier.Parse(ref parser) is not Identifier identifier)
        {
            return new ExpectedIdentifierError { Tokens = current.AdvanceTo(parser) };
        }

        Modifiers modifiers = null;
        Type returns = null;
        if (parser.TryAdvance<Returns>())
        {
            modifiers = Modifiers.Parse(ref parser);
            returns = Type.Unresolved.Parse(ref parser);
        }

        if (Scope.Definition.Parse(ref parser) is not Scope definition)
        {
            return new ExpectedDefinitionError { Tokens = current.AdvanceTo(parser) };
        }

        current = parser;
        return new Function
        {
            Keyword = keyword,
            Identifier = identifier,
            Modifiers = modifiers,
            Returns = returns,
            Definition = definition
        };
    }

    [ExcludeFromCodeCoverage]
    public override void ResolveReferences(Scope context)
    {
        Identifier.ResolveReferences(context);
        if (Returns is Type.Unresolved unresolved)
        {
            var member = context.Find(unresolved.Reference);
            Returns = member as Type ?? new Type.Calculated { Member = member };
        }
        Definition.ResolveReferences(context);
    }

    public class ExpectedIdentifierError : Function, IError
    {
        public string Reason { get; } = "expected identifier";
        public System.ReadOnlyMemory<Token> Tokens { get; init; }
    }

    public class ExpectedDefinitionError : Function, IError
    {
        public string Reason { get; } = "expected definition";
        public System.ReadOnlyMemory<Token> Tokens { get; init; }
    }
}