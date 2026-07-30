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

            // §4.7's two shapes: a run of words, or ONE anonymous value with its
            // indexer. The second was missing, so «{ 1, 2 } [0]» and «x => { … }
            // [0]» fell back to the value alone and the indexer became a
            // statement of its own with nothing to say it had been separated.
            //
            // The name requirement stays, and it is load bearing: it is what
            // keeps «{ 1 } { 2 }» from being one reference, which is the same
            // two-values-with-no-separator the aggregate rule refuses.
            if (reference.IsIndexed)
            {
                current = parser;
                return new Unresolved { Reference = reference };
            }

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
