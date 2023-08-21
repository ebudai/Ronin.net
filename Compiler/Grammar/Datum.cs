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
internal class Datum : Definition.Member
{
    public Keyword Mutability { get; init; }
    public Datatype Datatype { get; init; }
    public Value Initializer { get; init; }

    public class Declaration : Statement, IParsableSyntax<Declaration>
    {
        public Mutability Mutability { get; init; }
        public Identifier Identifier { get; init; }
        public Modifiers Modifiers { get; init; }
        public Reference Datatype { get; init; }
        public Value Initializer { get; init; }

        public override bool Equals(object obj) => (obj as Declaration)?.Datatype.Equals(Datatype) ?? false;

        public override int GetHashCode() => Datatype.GetHashCode();

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

            var initializer = parser.TryAdvance<Assign>() ? Value.Parse(ref parser) : null;

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

    public class Unresolved : Datum
    {
        public required Reference Reference { get; set; }
    }
}