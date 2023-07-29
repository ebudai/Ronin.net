// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;

namespace Ronin.Grammar;

internal class Datum : Definition.Member
{
    public Keyword Mutability { get; init; }
    public Datatype Datatype { get; init; }
    public Value Initializer { get; init; }

    /// <summary>
    ///     A singular piece of data residing in memory, and declared in a <see cref="Scope"/>
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
    public class Declaration : Statement, IParsableSyntax<Declaration>
    {
        public Mutability Mutability { get; init; }
        public required Name Name { get; init; }
        public Modifiers Modifiers { get; init; }
        public Reference Datatype { get; init; }
        public Value Initializer { get; init; }

        public new static Declaration Parse(ref Parser current)
        {
            Parser parser = current;

            var mutability = parser.Token as Mutability;
            if (mutability is not null) parser.Advance();

            if (Name.Parse(ref parser) is not Name name) return null;

            Modifiers modifiers = null;
            Reference datatype = null;
            if (parser.Token is Returns)
            {
                parser.Advance();
                modifiers = Modifiers.Parse(ref parser);
                datatype = Reference.Parse(ref parser);
            }

            Value initializer = null;
            if (parser.Token is Assign)
            {
                parser.Advance();
                initializer = Value.Parse(ref parser);
            }

            return new Declaration
            {
                Mutability = mutability,
                Name = name,
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