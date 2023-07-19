// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Grammar.Compound;
using Ronin.Lexicon.Keywords;
using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar;

internal class Datatype
{
    public Modifiers Modifiers { get; init; }
    public Algebra Algebra { get; init; }
    public Definition Definition { get; init; }

    private Datatype() { }

    public Datatype(Declaration declaration)
    {
        Algebra = new Algebra.Unresolved { Reference = declaration.Algebra };
        Definition = declaration.Definition;
    }

    /// <summary>
    ///     Restricts a <see cref="Datum"/> to a particular shape of data
    ///     resulting from evaluation of a <see cref="FunctionDeclaration"/> or <see cref="Datum"/>
    /// </summary>
    /// 
    /// <example>
    ///     datatype Car = Vehicle and { var speed => number; var price => money; }
    /// </example>
    public class Declaration : Statement, IParsableSyntax<Declaration>
    {
        public bool Extends { get; init; }
        public Identifier Name { get; init; }
        public Reference Algebra { get; init; }
        public Definition Definition { get; init; }

        public new static Declaration Parse(ref Parser current)
        {
            Parser parser = current;

            bool isExtension = parser.TryAdvance<Extends>();

            if (parser.TryAdvance<Lexicon.Keywords.Datatype>() is false) return null;

            if (Identifier.Parse(ref parser) is not Identifier name) return null;

            Reference algebra = null;
            if (parser.Token is Assign)
            {
                parser.Advance();
                algebra = Reference.Parse(ref parser);
            }

            var definition = Definition.Parse(ref parser);

            return new Declaration
            {
                Extends = isExtension,
                Name = name,
                Algebra = algebra,
                Definition = definition,
                Source = parser.Commit(ref current)
            };
        }
    }

    public class Unresolved : Datatype
    {
        public Reference Reference { get; init; }
    }
}

internal class Algebra
{
    public List<Datatype> Bases { get; } = new();
    public List<Datatype> Unions { get; } = new();

    public class Unresolved : Algebra
    {
        public Reference Reference { get; init; }
    }
}
