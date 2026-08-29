// Copyright © 2026 Eric Budai

using Ronin.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Integration;

/// <summary>
///     An action is admitted in no VALUE POSITION (FIVE-RULINGS §2b), reported by ONE
///     grammar-driven walk over the complete set of value positions — an operand, a call
///     argument, a list or lookup entry, a round-group part, a datum initializer, a condition,
///     an iterable (VALUE-POSITIONS-RULING). A standalone action statement is performed, not
///     consumed, and stays legal. The position function is total over grammar and resolved node
///     kinds, so a construct with no case fails the gate rather than silently admitting one.
/// </summary>
[Trait(nameof(Compilation), null)]
public class ValuePositions
{
    private static IReadOnlyList<Finding> Of(string source)
        => Compilation.Of(new SourceText(source, "Player.ron")).Findings;

    // «act» answers with no value, so «act 1» is an action call throughout.
    private const string Act = "function act (x => number) { return; }\n";

    private static void Inadmissible(string body) =>
        Assert.IsType<ActionInValue>(Assert.Single(Of(Act + body).OfType<ActionInValue>().Cast<Finding>()));

    [Fact(DisplayName = "an action is caught in every value position the checker already knew")]
    public void AnActionIsCaughtInEveryValuePositionTheCheckerAlreadyKnew()
    {
        Inadmissible("var r => number = act 1;\n");                                        // a typed initializer
        Inadmissible("function send (n => number) { return n; }\nvar r = send (act 1);\n"); // a typed call argument
        Inadmissible("function f => number { return act 1; }\n");                          // a written-return answer
        Inadmissible("var xs => list of number = [act 1];\n");                             // a typed list element
        Inadmissible("var a => number;\nvar r => truth = act 1 is a;\n");                  // an operator operand
    }

    [Fact(DisplayName = "an action is caught with no expected type — the peer, the operator, the container")]
    public void AnActionIsCaughtWithNoExpectedType()
    {
        Inadmissible("var u = 5;\nvar r = act 1 is u;\n");                                 // an untyped operand peer
        Inadmissible("var n => number;\nvar r => number = act 1 otherwise n;\n");          // «otherwise», which has no typer
        Inadmissible("function use (x) => number { return 1; }\nvar r => number = use (act 1);\n"); // a generic parameter
        Inadmissible("function outer { return act 1; }\n");                                // an omitted-return answer
        Inadmissible("var r = act 1;\n");                                                  // an untyped initializer root
        Inadmissible("var r = (act 1);\n");                                                // and through a round group
        Inadmissible("function use (x) => number { return 1; }\nvar r => number = use ((act 1, 2));\n"); // a multi-input group
        Inadmissible("function actions (x => number) { return [act x]; }\n");              // a list an inferred call exports
        Inadmissible("function values (x => number) { return [1 = act x]; }\n");           // a lookup value, keyed the same way
        Inadmissible("var q => number;\nvar r = (q = act 1, 2);\n");                       // an association among a group's inputs
    }

    [Fact(DisplayName = "a module with a parse error is not walked — no spurious action finding")]
    public void AModuleWithAParseErrorIsNotWalked()
    {
        // A parse error suppresses every later phase (findings-suppress-checking, Declare), so the
        // walk never runs on a module holding a recovery node and cannot report against one. The
        // «Malformed» finding is the whole of the diagnosis; no «ActionInValue» joins it.
        var findings = Of("function f => number { var ; return 1; }\n");
        Assert.Contains(findings, finding => finding is Malformed);
        Assert.Empty(findings.OfType<ActionInValue>());
    }

    [Fact(DisplayName = "an action is caught in a condition, a loop, and an iterable — the constructs the set missed")]
    public void AnActionIsCaughtInAConditionALoopAndAnIterable()
    {
        Inadmissible("if act 1 { }\n");                          // an «if» condition
        Inadmissible("while act 1 { }\n");                       // a «while» condition
        Inadmissible("var ready => number;\nwhen act 1 { }\n");  // a «when» condition
        Inadmissible("for each y in act 1 { }\n");               // a «for each» iterable
    }

    [Fact(DisplayName = "a standalone action statement is performed, not consumed, and stays legal")]
    public void AStandaloneActionStatementIsPerformedNotConsumedAndStaysLegal()
    {
        // Performing an action is the ordinary use of one, so a bare «act 1;» reports nothing — its
        // root is not a value position, and «(A)» needs no carve-out to keep it legal. But its own
        // inner positions are still value positions: «send (act 1);» reports the argument.
        Assert.Empty(Of(Act + "act 1;\n").OfType<ActionInValue>());
        Assert.Empty(Of(Act + "(act 1);\n").OfType<ActionInValue>());
        Inadmissible("function send (n => number) { return n; }\nsend (act 1);\n");
    }

    [Fact(DisplayName = "a value's own action reports once, at the action, not twice")]
    public void AValuesOwnActionReportsOnceAtTheActionNotTwice()
    {
        // Round grouping is transparent and «Disagreeing» no longer emits «ActionInValue», so one
        // action is one finding, whatever brackets or type checks surround it.
        Assert.Single(Of(Act + "function send (n => number) { return n; }\nvar r = send (act 1);\n"));
        Assert.Single(Of(Act + "function send (n => number) { return n; }\nvar r = send ((act 1));\n"));
    }

    // ---- the totality gate: the position function has a case for every node kind ----------------

    private sealed class Unclassifiable : Node
    {
        public override bool Alike(Node other) => false;
        protected override string Render() => string.Empty;
        protected override int Hash() => 0;
    }

    [Fact(DisplayName = "the value-position classifiers are total — an unclassified kind throws, not admits")]
    public void TheValuePositionClassifiersAreTotal()
    {
        // A node kind with no case must FAIL, not silently admit an action (VALUE-POSITIONS-RULING
        // §2). The «none» kinds are explicit «=> []» arms a reader reviews, distinct in spelling
        // from this «_ => throw» that a kind reaches only by being unclassified (§4). A bare
        // «Grammar.Value» — a base the parser never builds — is neither a statement value position
        // nor a resolved value part, and an «Unclassifiable» node is no resolved value position:
        // each is a kind with no case, and each throws rather than admitting whatever it holds.
        Assert.Throws<InvalidOperationException>(() => Compilation.Positions.ValuesOf(new Ronin.Grammar.Value()));
        Assert.Throws<InvalidOperationException>(() => Compilation.Positions.PartsOf(new Ronin.Grammar.Value(), false));
        Assert.Throws<InvalidOperationException>(() => Compilation.Positions.Within(new Unclassifiable()).ToList());
    }

    // A recovery node never reaches the walk — a module with any parse error is diagnosed and
    // never checked (findings-suppress-checking, Declare) — so every IError kind is out of the
    // statement classifier's domain. Beside them, three concrete grammar kinds the parser never
    // hands the walk as a statement: the two abstract-in-practice bases «Member» and «Value», and
    // a «Collection.Element», which is reached through its collection, not as a statement.
    private static readonly Type[] OutsideTheStatementDomain =
        [typeof(Ronin.Grammar.Member), typeof(Ronin.Grammar.Value), typeof(Ronin.Grammar.Collection.Element)];

    [Fact(DisplayName = "every grammar statement kind reaches a classifier case — a new construct fails this")]
    public void EveryGrammarStatementKindReachesAClassifierCase()
    {
        // The gate the ruling asks for (§2, §3): a construct added without a value-position case
        // fails the BUILD, the moment it is added, not only when source reaches it. Every concrete
        // grammar statement kind is enumerated and handed to the classifier on a field-less
        // instance — which reads but never dereferences its fields. A recovery node never reaches
        // the walk, so an IError kind is skipped, arm or none. Every other kind must reach a case
        // (return) rather than the «_ => throw» — except the reviewed out-of-domain kinds, which
        // must throw, so the exclusion cannot quietly hide a kind that was left unclassified.
        var kinds = typeof(Compilation).Assembly.GetTypes()
                        .Where(type => typeof(Ronin.Grammar.Statement).IsAssignableFrom(type)
                                    && type is { IsAbstract: false, IsInterface: false, ContainsGenericParameters: false });

        foreach (var kind in kinds)
        {
            if (typeof(IError).IsAssignableFrom(kind)) continue;

            var instance = (Ronin.Grammar.Statement)RuntimeHelpers.GetUninitializedObject(kind);

            var cased = true;
            try { Compilation.Positions.ValuesOf(instance); }
            catch (InvalidOperationException) { cased = false; }

            if (OutsideTheStatementDomain.Contains(kind))
                Assert.False(cased, $"{kind.FullName} is reviewed out-of-domain but reached a case");
            else
                Assert.True(cased, $"{kind.FullName} reached no classifier case — classify it (§2)");
        }
    }

    [Fact(DisplayName = "every resolved node kind is a classifier case — a new node kind fails this")]
    public void EveryResolvedNodeKindIsAClassifierCase()
    {
        // The resolved-node classifier's gate (§3), pinned by the type set rather than by a
        // field-less instance — a resolved node dereferences its children, so an uninitialised one
        // cannot be handed «Within». A node kind added to the tree changes this set and fails here
        // until «Children» gains a case for it; «TheValuePositionClassifiersAreTotal» proves an
        // unclassified kind throws, and the walk tests prove each case is read correctly.
        var kinds = typeof(Compilation).Assembly.GetTypes()
                        .Where(type => typeof(Node).IsAssignableFrom(type)
                                    && type is { IsAbstract: false, IsInterface: false, ContainsGenericParameters: false })
                        .Select(type => type.Name)
                        .OrderBy(name => name);

        Assert.Equal(["Binding", "Call", "Group", "Literal", "Name", "Operation", "Previous"], kinds);
    }
}
