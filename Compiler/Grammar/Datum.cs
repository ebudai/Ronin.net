// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;

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
internal class Datum : Context.Member
{
    public Mutability Mutability { get; init; }
    public Datatype Datatype { get; set; }
    public Value Initializer { get; init; }

    public class Declaration : Statement, IParsableSyntax<Declaration>
    {
        public Mutability Mutability { get; init; }
        public Identifier Identifier { get; init; }
        public Modifiers Modifiers { get; init; }
        public Reference Datatype { get; init; }
        public Value Initializer { get; init; }

        public new static Declaration Parse(ref Parser current)
        {
            Parser parser = current;

            var mutability = parser.Token as Mutability;
            if (mutability is not null) parser.Advance();

            if (Identifier.Parse(ref parser) is not Identifier identifier) return null;

            Modifiers modifiers = null;
            Reference datatype = null;
            if (parser.TryAdvance<Returns>())
            {
                modifiers = Modifiers.Parse(ref parser);
                datatype = Reference.Parse(ref parser);
            }

            Value initializer = null;
            if (parser.TryAdvance<Assign>()) initializer = Value.Parse(ref parser);

            if (datatype is null)
            {
                if (mutability is null || initializer is null) return null;
            }

            return new Declaration
            {
                Mutability = mutability,
                Identifier = identifier,
                Modifiers = modifiers ?? new(),
                Datatype = datatype,
                Initializer = initializer,
                Source = parser.Commit(ref current)
            };
        }
    }

    public new class Unresolved : Datum
    {
        public required Reference Reference { get; set; }
    }
}