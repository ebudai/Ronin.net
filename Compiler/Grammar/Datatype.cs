// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Grammar.Compound;
using Ronin.Lexicon.Symbols;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Grammar;

internal class Datatype
{
    [ExcludeFromCodeCoverage] public Modifiers Modifiers { get; init; }
    [ExcludeFromCodeCoverage] public Algebra Algebra { get; init; }
    [ExcludeFromCodeCoverage] public Definition Definition { get; init; }

    /// <summary>
    ///     Restricts a <see cref="Datum"/> to a particular shape of data
    ///     resulting from evaluation of a <see cref="Function.Declaration"/> or <see cref="Datum"/>
    /// </summary>
    /// 
    /// <example>
    ///     datatype Car = Vehicle and { var speed => number; var price => money; }
    /// </example>
    public class Declaration : Scope, IParsableSyntax<Declaration>
    {
        public Identifier Name { get; init; }
        public Reference Algebra { get; init; }

        public new static Declaration Parse(ref Parser current)
        {
            Parser parser = current;

            var modifiers = Modifiers.Parse(ref parser);

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
                Modifiers = modifiers,
                Name = name,
                Algebra = algebra,
                Definition = definition,
                Source = parser.Commit(ref current)
            };
        }
    }

    [ExcludeFromCodeCoverage]
    public class Unresolved : Datatype
    {
        public Reference Reference { get; init; }
    }
}

[ExcludeFromCodeCoverage]
internal class Algebra
{
    public List<Datatype> Bases { get; } = new();
    public List<Datatype> Unions { get; } = new();

    public class Unresolved : Algebra
    {
        public Reference Reference { get; init; }
    }
}
