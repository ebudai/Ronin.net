// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Grammar;

internal class Member : Value, IParsable<Member>
{
    public Modifiers Modifiers { get; init; }
    public Identifier Identifier { get; init; }

    public static new Member Parse(ref Parser current)
        => Datum.Parse(ref current)
        ?? Function.Parse(ref current)
        ?? Type.Parse(ref current) as Member;

    public class Unresolved : Member
    {
        public Reference Reference { get; init; }

        public static new Unresolved Parse(ref Parser current)
        {
            Parser parser = current;

            if (Reference.Parse(ref parser) is not Reference reference) return null;

            foreach (var component in reference)
            {
                if (component.AsName is not null)
                {
                    current = parser;
                    return new Unresolved { Reference = reference };
                }
            }

            return null;
        }
    }
}
