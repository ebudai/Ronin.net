// Copyright © 2026 Eric Budai

using Ronin.Compiler;

namespace Unit;

/// <summary>
///     No name whose own span reads as something else is admitted as a declaration.
/// </summary>
///
/// <remarks>
///     <para>
///     The declaration half of the same property the repair coverage rests on. A
///     name is refused when its own words already mean a call or a comparison —
///     «send a» is a call, «a is b» a comparison — because no bracketing selects
///     a name over a span that reads as something else, so the declaration would
///     be one nobody could ever write. The rules that refuse them
///     («Rules.ReadsAs», «Rules.Infixes») are hand-written span analyses, and
///     this asks the RESOLVER — a different subsystem — whether they let one
///     through.
///     </para>
///     <para>
///     ONE DIRECTION, because only one is an invariant. Admission is stricter
///     than writability on purpose: «ReadsAs» refuses any name whose WORDS could
///     fit a pattern, whether or not this scope declares the arguments that would
///     complete the call — «send send» is refused though «send» is no name here.
///     Within a module the repair is a rename and the author owns both sides, so
///     the rule over-refuses rather than track which scope makes which reading
///     real. So a refused name need not be unwritable; but an ADMITTED one must
///     be writable, or it is a declaration that resolves as something else the
///     moment it is used — and that is the direction checked here.
///     </para>
///     <para>
///     GENERATED through production «Compilation», with the forbidden candidates
///     generated rather than listed: every sequence over a vocabulary that can
///     form calls and comparisons is declared and compiled, and the ones the
///     rules refuse are counted, not omitted. The exact counts are asserted
///     because a safety property checked over only admitted names passes forever
///     if the generator stops producing the unwritable ones.
///     </para>
/// </remarks>
[Trait(nameof(Compilation), null)]
public class DeclarationAdmission
{
    private const string Patterns =
        "function send (x => number) { return x; }\n"
      + "function send (x => number) to (y => number) { return x; }\n";

    private const string Operands = "var a => number;\nvar b => number;\n";

    /// <summary>Two operands, an anchor, an operator, and glue — enough to form calls and comparisons.</summary>
    private static readonly string[] words = ["a", "b", "send", "is", "to"];

    private static IEnumerable<string> Candidates()
    {
        List<string[]> level = [[]];

        for (var length = 1; length <= 4; ++length)
        {
            List<string[]> next = [];

            foreach (var prefix in level)
                foreach (var word in words)
                    next.Add([.. prefix, word]);

            foreach (var candidate in next) yield return string.Join(' ', candidate);

            level = next;
        }
    }

    /// <summary>Whether the candidate's own words, once it is a name, read as only that name.</summary>
    ///
    /// <remarks>
    ///     Declared alongside the operands and patterns, so its span is offered
    ///     every rival reading — a call to «send _», a comparison across «is» —
    ///     and reads as itself only when none of them competes.
    /// </remarks>
    private static bool ReadsAsItself(string candidate)
    {
        SymbolTable symbols = new();

        symbols.WithNames(["a", "b", candidate]).WithPatterns(["send _", "send _ to _"]);

        var own = new Resolver(symbols).Resolve(Lexemes.Lex(candidate));

        return own.Kind is ResolutionKind.Resolved && own.Reading == $"«{candidate}»";
    }

    private static bool Refused(string candidate)
        => Compilation.Of(new SourceText(Patterns + Operands + $"var {candidate} => number;\n", "gen.ron")).Findings.Count is not 0;

    [Fact(DisplayName = "a name is admitted only if its own span reads as itself")]
    public void ANameIsAdmittedOnlyIfItsOwnSpanReadsAsItself()
    {
        // The context is clean, so a finding on the source is a finding on the
        // candidate and nothing else.
        Assert.Empty(Compilation.Of(new SourceText(Patterns + Operands, "gen.ron")).Findings);

        List<string> leaked = [];
        var refused = 0;
        var admitted = 0;
        var itself = 0;

        foreach (var candidate in Candidates())
        {
            if (ReadsAsItself(candidate)) ++itself;

            if (Refused(candidate)) { ++refused; continue; }

            ++admitted;

            // Admitted, so it must read as itself. One that does not is a name
            // the compiler let you declare and the resolver reads as a call or a
            // comparison the moment you write it.
            if (ReadsAsItself(candidate) is false) leaked.Add($"«{candidate}» was admitted but does not read as itself");
        }

        Assert.Empty(leaked);

        // Exact, so the space keeps both the names that read as themselves and
        // the ones that do not — the property is vacuous without the second.
        Assert.Equal(780, refused + admitted);
        Assert.Equal(357, refused);
        Assert.Equal(423, admitted);
        Assert.Equal(758, itself);
    }
}
