// Copyright © 2026 Eric Budai

using Ronin.Compiler;

namespace Unit;

/// <summary>
///     Every displayed reading of a generated TYPE ambiguity has a repair that
///     selects it.
/// </summary>
///
/// <remarks>
///     <para>
///     The type half of <see cref="RepairCoverage"/>, and it exists because that
///     one does not reach here: it generates value calls and names, so its
///     ambiguities are never an operation against a call, and the ruled three-way
///     «lookup text =&gt; number =&gt; truth» — three readings, one an operation
///     the search treated as the whole statement and dropped — passed its suite
///     with two repairs. A compact product over the scalar types, «optional»,
///     «list of», «lookup», bare arrow chains, and keyed groups exposes that class
///     and guards the structural relationship rather than one spelling.
///     </para>
///     <para>
///     THROUGH the resolver in type mode, because the parser only admits the arrow
///     inside an annotation and this composes arrows freely. Each ambiguity's
///     displayed readings are matched one to one against the repairs the search
///     produced, each repair applied to a re-resolved unique tree compared by
///     shape with «Node.Same» — the same contract the value property holds, asked
///     of the grammar the checker will read.
///     </para>
/// </remarks>
[Trait(nameof(Resolver), null)]
public class TypeRepairCoverage
{
    private static readonly string[] atoms = ["number", "text", "truth", "error"];

    private static Resolver Types() => new(new SymbolTable(), kind: SymbolKind.Type);

    /// <summary>
    ///     A random type expression: a scalar, or a constructor, arrow, or keyed
    ///     group over shallower ones — the compositions the arrow ambiguities live
    ///     in.
    /// </summary>
    private static string Type(Random random, int depth)
    {
        if (depth <= 0) return atoms[random.Next(atoms.Length)];

        return random.Next(6) switch
        {
            0 => atoms[random.Next(atoms.Length)],
            1 => "optional " + Type(random, depth - 1),
            2 => "list of " + Type(random, depth - 1),
            3 => "lookup " + Type(random, depth - 1) + " => " + Type(random, depth - 1),
            4 => Type(random, depth - 1) + " => " + Type(random, depth - 1),
            _ => "( " + Type(random, depth - 1) + " = " + Type(random, depth - 1) + " )",
        };
    }

    [Fact(DisplayName = "every displayed reading of a generated type ambiguity has a repair that structurally selects it")]
    public void EveryDisplayedReadingOfAGeneratedTypeAmbiguityHasARepairThatStructurallySelectsIt()
    {
        Random random = new(Seed: 20260815);
        var ambiguities = 0;

        for (var trial = 0; trial < 2000; ++trial)
        {
            var source = Type(random, depth: 3);
            var lexemes = Lexemes.Lex(source);
            var resolver = Types();
            var resolution = resolver.Resolve(lexemes);

            if (resolution.Kind is not ResolutionKind.Ambiguous) continue;

            ++ambiguities;

            // The competing readings as trees, and the repairs in the order the
            // search produced them — one per tree. Fewer means the search dropped a
            // meaning it showed, which is exactly the operation-versus-call gap.
            var alternatives = resolution.Alternatives;
            var repairs = Repairs.For(resolver, lexemes, resolution);

            Assert.Equal(alternatives.Count, repairs.Count);

            List<Node> selected = [];

            foreach (var (alternative, repair) in alternatives.Zip(repairs))
            {
                // Re-resolved through the SAME resolver the alternative came from,
                // because an operation is «Same» only by the very operator instance
                // the resolver chose, and each resolver builds its own arrow. The
                // value property never meets this — its generated grammar has no
                // operators — so this is the type half's own care.
                var reresolved = resolver.Resolve(Lexemes.Lex(Applied(source, repair)));

                // It applies to a uniquely-resolved type — no ambiguity left.
                Assert.True(reresolved.TryTree(out var tree));

                // And selects the TREE it was built for, by shape, its brackets
                // stripped the way the search compares a candidate to its target.
                var stripped = Stripped(tree);

                Assert.Equal(Stripped(alternative), stripped, Node.Same);

                selected.Add(stripped);
            }

            // Distinct shapes, so the repairs cover the displayed meanings one to
            // one rather than two edits selecting one while another goes unrepaired.
            Assert.Equal(repairs.Count, selected.Distinct(Node.Same).Count());
        }

        // Met the ambiguities to prove the property over — a completeness property
        // proved over none passes forever, and the count catches a generator that
        // stops producing them.
        Assert.Equal(730, ambiguities);
    }

    /// <summary>The source with one repair's brackets typed in, right to left.</summary>
    private static string Applied(string source, Repair repair)
    {
        foreach (var insertion in repair.Insertions.OrderByDescending(insertion => insertion.At))
        {
            source = source[..insertion.At] + insertion.Text + source[insertion.At..];
        }

        return source;
    }

    /// <summary>
    ///     A tree with the brackets a repair added stripped away, the way the repair
    ///     search compares a candidate to its target — into calls, operations, and
    ///     keyed groups, the shapes a type nests.
    /// </summary>
    private static Node Stripped(Node tree)
    {
        var bare = Bare(tree);

        return bare switch
        {
            Node.Call call => new Node.Call(call.Pattern, [.. call.Arguments.Select(Stripped)]),
            Node.Operation operation
                => new Node.Operation(Stripped(operation.Left), operation.Symbol, operation.Operator, Stripped(operation.Right)),
            Node.Group { Kind: Node.Grouping.Keyed } keyed
                => new Node.Group([.. keyed.Parts.Select(part => new Node.Entry(Stripped(part.Key), Stripped(part.Value)))],
                                  Node.Grouping.Keyed),
            _ => bare,
        };
    }

    private static Node Bare(Node tree)
        => tree is Node.Group { Kind: Node.Grouping.Group, Parts.Count: 1 } group ? Bare(group.Parts[0].Value) : tree;
}
