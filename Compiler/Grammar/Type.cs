// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;
using System.Collections.Generic;

namespace Ronin.Grammar;
/// <summary>
///     Restricts a <see cref="Datum"/> to a particular shape of data
///     resulting from evaluation of a <see cref="Function.Declaration"/> or <see cref="Datum"/>
/// </summary>
/// 
/// <example>
///     datatype Car = Vehicle and { var speed => number; var price => money; }
/// </example>
internal class Type : Value, IGrammar<Type>
{
    public Algebra Algebra { get; set; }
    public Context Definition { get; init; }
    public Identifier Identifier { get; init; }

    public static new Type Parse(ref Parser current)
    {

    }

    

    public class Declaration : Scope, IGrammar<Declaration>
    {
        public Identifier Identifier { get; init; }
        public Reference Algebra { get; init; }

        public new static Declaration Parse(ref Parser current)
        {
            Parser parser = current;

            var modifiers = Modifiers.Parse(ref parser);

            if (parser.TryParse<Lexicon.Type>() is null) return null;

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

        public new void Define(Context context, List<Error> errors)
        {
            Definition.Define(context, errors);

            Type datatype = new()
            {
                Modifiers = Modifiers,
                Algebra = new Algebra.Unresolved { Reference = Algebra },
                Definition = Definition
            };

            if (context.Add(Identifier, datatype) is Error error) errors.Add(error);
        }
    }

    public new class Unresolved : Type
    {
        public Reference Reference { get; init; }
    }

    public new class Overloaded : Type
    {
        public List<Resolution> Overloads { get; init; }
    }

    public class Calculated<T> : Type where T : Context.Member
    {
        public T Member { get; init; }
    }
}

public class Algebra
{
    public List<Type> Bases { get; } = new();
    public List<Type> Unions { get; } = new();

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