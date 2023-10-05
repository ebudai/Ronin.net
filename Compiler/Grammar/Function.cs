// Copyright © 2023 Eric Budai

using OneOf;
using Ronin.Compiler;
using Ronin.Lexicon;

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
internal class Function : Statement, IGrammar<Function>
{
    public Lexicon.Function Keyword { get; init; }
    public Identifier Identifier { get; init; }
    public Modifiers Modifiers { get;init; }
    public Type Returns { get; set; }
    public Definition Definition { get; init; }    
    
    public new static Function Parse(ref Parser current)
    {
        Parser parser = current;

        var keyword = parser.Token;
        if (keyword is not Lexicon.Function) return null;
        parser.Advance();

        if (Identifier.Parse(ref parser) is not Identifier identifier) return null;

        Modifiers modifiers = null;
        Reference returns = null;
        if (parser.TryAdvance<Returns>())
        {
            modifiers = Modifiers.Parse(ref parser);
            returns = Reference.Parse(ref parser);
        }

        var definition = Definition.Parse(ref parser);

        current = parser;
        return new Function
        {
            Identifier = identifier,
            Modifiers = modifiers,
            Returns = new Type.Unresolved { Reference = returns },
            Definition = definition,
        };
    }

    public class Unresolved : Function
    {
        public Reference Reference { get; init; }
    }
}