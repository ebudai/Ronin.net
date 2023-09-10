// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;
using System.Collections.Generic;

namespace Ronin.Grammar;

internal class Datatype : Context.Member
{
    public Algebra Algebra { get; set; }
    public Context Definition { get; init; }    

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
        public Identifier Identifier { get; init; }
        public Reference Algebra { get; init; }

        public new static Declaration Parse(ref Parser current)
        {
            Parser parser = current;

            var modifiers = Modifiers.Parse(ref parser);

            if (parser.TryAdvance<Lexicon.Datatype>() is false) return null;

            if (Identifier.Parse(ref parser) is not Identifier name) return null;

            Reference algebra = null;
            if (parser.TryAdvance<Assign>()) algebra = Reference.Parse(ref parser);

            var definition = Context.Parse(ref parser);

            return new Declaration
            {
                Modifiers = modifiers,
                Identifier = name,
                Algebra = algebra,
                Definition = definition,
                Source = parser.Commit(ref current)
            };
        }
    }

    public new class Unresolved : Datatype
    {
        public Reference Reference { get; init; }
    }

    public new class Overloaded : Datatype
    {
        public List<Resolution> Overloads { get; init; }
    }

    public class Calculated<T> : Datatype where T : Context.Member
    {
        public T Member { get; init; }
    }
}

internal class Algebra : Syntax
{
    public List<Datatype> Bases { get; } = new();
    public List<Datatype> Unions { get; } = new();

    public class Unresolved : Algebra
    {
        public Reference Reference { get; init; }
    }

    public class Overloaded : Algebra
    {
        public List<Resolution> Overloads { get; init; }
    }

    public class Calculated<T> : Algebra where T : Context.Member
    {
        public T Member { get; init; }
    }
}
