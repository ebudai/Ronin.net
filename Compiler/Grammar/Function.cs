// Copyright © 2023 Eric Budai

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
internal class Function : Member
{
    public Lexicon.Function Keyword { get; init; }
    public Type Returns { get; set; }
    public Scope Definition { get; init; }

    public new static Function Parse(ref Parser current)
    {
        Parser parser = current;

        if (parser.TryAdvance<Lexicon.Function>(out var keyword) is false) return null;

        if (Identifier.Parse(ref parser) is not Identifier identifier)
        {
            return new ExpectedIdentifierError { Tokens = Parser.Recover(ref current, parser) };
        }

        Modifiers modifiers = null;
        Type returns = null;
        if (parser.TryAdvance<Returns>())
        {
            modifiers = Modifiers.Parse(ref parser);

            // A consumed «=>» commits. Leaving the type optional afterwards made
            // «function f => {}» a function with no return type rather than one
            // missing it, so the mistake became indistinguishable from the
            // untyped form the language already has.
            if (Heading.Of(ref parser, Type.Unresolved.Parse) is not Type declared)
            {
                return new ExpectedTypeError { Tokens = Parser.Recover(ref current, parser) };
            }

            returns = declared;
        }

        if (Scope.Definition.Parse(ref parser) is not Scope definition)
        {
            return new ExpectedDefinitionError { Tokens = Parser.Recover(ref current, parser) };
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

    public class ExpectedTypeError : Function, IError
    {
        public string Reason { get; } = $"expected a type after '{Lexicon.Returns.symbol}'";
        public System.ReadOnlyMemory<Token> Tokens { get; init; }
    }
}
