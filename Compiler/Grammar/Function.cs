// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;
using System.Runtime.CompilerServices;

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
    public Identifier Identifier { get; init; }
    public Modifiers Modifiers { get; init; }
    public Type Returns { get; set; }
    public Scope Definition { get; init; }

    public new static Function Parse(ref Parser current)
    {
        Parser parser = current;

        var keyword = parser.Token;
        if (keyword is not Lexicon.Function) return null;
        parser.Advance();

        if (Identifier.Parse(ref parser) is not Identifier identifier) return null;

        Modifiers modifiers = null;
        Type returns = null;
        if (parser.TryAdvance<Returns>())
        {
            modifiers = Modifiers.Parse(ref parser);
            returns = Type.Unresolved.Parse(ref parser);
        }

        Statement definition = null;
        if (parser.Token is Assign)
        {
            parser.Advance();
            definition = Value.Parse(ref parser);    
        }
        definition ??= Scope.Parse(ref parser);

        current = parser;
        return new Function
        {
            Identifier = identifier,
            Modifiers = modifiers,
            Returns = returns,
            Definition = definition as Scope ?? new Scope { definition }
        };
    }

    public new class Unresolved : Function
    {
        public Reference Reference { get; init; }

        public static new Function Parse(ref Parser current)
        {
            if (Reference.Parse(ref current) is not Reference reference) return null;
            return new Unresolved { Reference = reference };
        }
    }
}