// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Symbols;
using Ronin.Lexicon.Keywords;

namespace Ronin.Grammar;

internal class Datum
{
    public Keyword Mutability { get; init; }
    public Modifiers Modifiers { get; init; }
    public Datatype Datatype { get; init; }
    public Value Initializer { get; init; }

    private Datum() { }

    public Datum(Declaration declaration)
    {
        Mutability = declaration.Mutability;
        Modifiers = declaration.Modifiers;

        Datatype = new Datatype.Unresolved 
        {
            Modifiers = declaration.Modifiers,
            Reference = declaration.Datatype 
        };

        Initializer = declaration.Initializer as Anonymous ?? new Value.Unresolved(declaration.Initializer) as Value;
    }

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
        public Keyword Mutability { get; init; }
        public Name Name { get; init; }
        public Modifiers Modifiers { get; init; }
        public Reference Datatype { get; init; }
        public Value Initializer { get; init; }

        public new static Declaration Parse(ref Parser current)
        {
            Parser parser = current;

            var mutator = parser.Token is Variable or Constant or Reactive ? parser.Token as Keyword : null;
            if (mutator is not null) parser.Advance();

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
                Mutability = mutator,
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
        public Reference Reference { get; }
    }
}
