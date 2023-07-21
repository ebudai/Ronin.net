// Copyright © 2023 Eric Budai

using Ronin.Grammar.Compound;
using Ronin.Lexicon.Symbols;
using Ronin.Compiler;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Grammar;

internal class Function
{
    [ExcludeFromCodeCoverage] public Modifiers Modifiers { get; init; }
    [ExcludeFromCodeCoverage] public Datatype Returns { get; init; }
    [ExcludeFromCodeCoverage] public Definition Definition { get; init; }

    /// <summary>
    ///     Ordered grouping of instructions to execute when called
    /// </summary>
    /// 
    /// <example>
    ///     function florb (x => number) things with (fast => maybe, fun => maybe) stuff => whole number 
    ///     {
    ///         return 8; 
    ///     }
    /// </example>
    public class Declaration : Scope, IParsableSyntax<Declaration>
    {
        public Identifier Identifier { get; init; }
        public Reference Returns { get; init; }

        public new static Declaration Parse(ref Parser current)
        {
            Parser parser = current;

            if (parser.TryAdvance<Lexicon.Keywords.Function>() is false) return null;

            if (Identifier.Parse(ref parser) is not Identifier identifier) return null;

            Modifiers modifiers = null;
            Reference returns = null;
            if (parser.Token is Returns)
            {
                parser.Advance();
                modifiers = Modifiers.Parse(ref parser);
                returns = Reference.Parse(ref parser);
            }

            var definition = Definition.Parse(ref parser);

            return new Declaration
            {
                Identifier = identifier,
                Modifiers = modifiers,
                Returns = returns,
                Definition = definition,
                Source = parser.Commit(ref current)
            };
        }
    }
}