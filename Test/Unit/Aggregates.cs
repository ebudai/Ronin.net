// Copyright © 2026 Eric Budai

using Ronin.Grammar;
using Ronin.Lexicon;
using Ronin.Runtime;

namespace Unit;

/// <summary>
///     «=» is an association separator, never an expression operator.
/// </summary>
///
/// <remarks>
///     The invariant <see cref="Collection"/> parses on. A «[…]» is one production
///     and one decision only because «=» inside brackets can mean exactly one
///     thing: were it an operator too, «is this an association?» could not be
///     answered without parsing the key both ways, and the exponential the single
///     parse removed would return through a door nobody is watching. The constraint
///     lived in a comment, which is the one place with no consumer — and the ladder
///     work is about to make the operator table user-extensible, which is the door.
/// </remarks>
[Trait(nameof(Collection), null)]
public class Aggregates
{
    [Fact(DisplayName = "= is an association separator, never an operator")]
    public void EqualsIsAnAssociationSeparatorNeverAnOperator()
        => Assert.False(Builtin.Operators.ContainsKey(Assign.symbol.ToString()),
            "«=» inside brackets is only ever an association separator, never an expression operator. "
          + "If that ever stops being true, «Collection»'s single parse becomes a guess and the exponential returns.");
}
