// Copyright © 2026 Eric Budai

using Ronin.Compiler;
using Ronin.Runtime;

namespace Unit;

/// <summary>
///     One cell per declared member holding N values, and a handle that is not
///     an index.
/// </summary>
[Trait(nameof(Graph), null)]
public class Instances
{
    private static Graph Boxes(int count, out Instance[] made)
    {
        Graph graph = new();
        graph.Type("Box", ("cash", 0d), ("label", ""));

        made = [.. Enumerable.Range(0, count).Select(_ => graph.Create("Box"))];

        return graph;
    }

    [Fact(DisplayName = "the graph is the size of the source, not the size of the world")]
    public void TheGraphIsTheSizeOfTheSourceNotTheSizeOfTheWorld()
    {
        // The decision, pinned. A comment would not survive an optimisation pass
        // and this fails the moment someone reintroduces per-instance nodes for
        // a plausible local reason, which is how a decision of this kind is
        // normally lost.
        //
        // Under grouped storage the dependency graph is the size of the SOURCE
        // TEXT; under per-instance nodes it is the size of the world. Everything
        // downstream inherits that — edges, dirty propagation, cascade analysis,
        // and every diagnostic that names a node.
        var one = Boxes(1, out _).Declared;
        var thousand = Boxes(1000, out _).Declared;

        Assert.Equal(one, thousand);
        Assert.Equal(2, one);
    }

    [Fact(DisplayName = "every instance has its own value in the shared cell")]
    public void EveryInstanceHasItsOwnValueInTheSharedCell()
    {
        var graph = Boxes(3, out var boxes);

        graph.Write("cash", boxes[1], 50d);
        graph.Step();

        Assert.Equal([0d, 50d, 0d], boxes.Select(box => graph.Read("cash", box)));
    }

    [Fact(DisplayName = "a handle survives the removal of something else")]
    public void AHandleSurvivesTheRemovalOfSomethingElse()
    {
        // Removal is swap-with-last, so the LAST instance changes index without
        // anything happening to it. An index held across that reads a different
        // instance and answers confidently — which is why what is held is a
        // handle, and why only the runtime ever sees the index.
        var graph = Boxes(3, out var boxes);

        graph.Write("cash", boxes[2], 90d);
        graph.Step();

        graph.Remove(boxes[0]);

        Assert.Equal(90d, graph.Read("cash", boxes[2]));
        Assert.Equal(0d, graph.Read("cash", boxes[1]));
    }

    [Fact(DisplayName = "and a handle to a removed one is refused, not answered")]
    public void AndAHandleToARemovedOneIsRefusedNotAnswered()
    {
        var graph = Boxes(2, out var boxes);

        graph.Remove(boxes[0]);
        graph.Step();

        // The slot is free — as of the round boundary, since removal is a write
        // — and the next instance takes it. Without the generation this read
        // would answer with that instance's value, which is a wrong answer
        // rather than a refusal: the failure class this project refuses
        // everywhere else, arriving through a data structure.
        var replacing = graph.Create("Box");

        graph.Write("cash", replacing, 7d);
        graph.Step();

        Assert.Equal(7d, graph.Read("cash", replacing));

        var stale = Assert.IsType<Error>(graph.Read("cash", boxes[0]));

        Assert.Contains("has been removed", stale.Message);
    }

    [Fact(DisplayName = "and one from another type is refused too")]
    public void AndOneFromAnotherTypeIsRefusedToo()
    {
        // The arrays are per type, so a handle from one population indexes
        // another's members perfectly well. Every one of those reads is wrong
        // and none of them would say so, which is why the type travels with it.
        Graph graph = new();
        graph.Type("Box", ("cash", 0d));
        graph.Type("Crate", ("weight", 0d));

        var box = graph.Create("Box");

        graph.Create("Crate");

        Assert.IsType<Error>(graph.Read("weight", box));
    }

    [Fact(DisplayName = "sibling types may share a member name")]
    public void SiblingTypesMayShareAMemberName()
    {
        // Found by audit, and it was the runtime disagreeing with the frontend.
        // A type body is its own scope, so this source is accepted with no
        // findings — and the runtime keyed a member on its bare spelling, so the
        // second «name» collided with the first through the global uniqueness
        // check. «name», «id» and «value» in unrelated types is not a corner
        // case.
        Assert.Empty(Compilation.Of(new SourceText("""
            type Box    { var name => Number; }
            type Person { var name => Number; }

            """, "Player.ron")).Findings);

        // and the runtime now represents what that source declares
        Graph graph = new();
        graph.Type("Box", ("name", 0d));
        graph.Type("Person", ("name", 1d));

        var box = graph.Create("Box");
        var person = graph.Create("Person");

        graph.Write("name", box, 7d);
        graph.Step();

        Assert.Equal(7d, graph.Read("name", box));
        Assert.Equal(1d, graph.Read("name", person));

        // two cells, still one per declared member
        Assert.Equal(2, graph.Declared);
    }

    [Fact(DisplayName = "and a type is declared once, whole or not at all")]
    public void AndATypeIsDeclaredOnceWholeOrNotAtAll()
    {
        Graph graph = new();
        graph.Type("Box", ("cash", 0d));

        Assert.Contains("already declared",
                        Assert.Throws<InitialisationFailure>(() => graph.Type("Box", ("weight", 0d))).Message);

        // Found by audit. Members were declared one at a time and published
        // after, so a collision on any but the first left the earlier nodes
        // behind with their seeds and the type itself absent.
        //
        // Qualifying the cells took most of that away: a member shares a
        // namespace only with its own type's members, so a module-level
        // «occupied» no longer collides with «Broken.occupied» at all. What
        // remains reachable is a member named twice, and it leaves nothing.
        Graph leaky = new();
        leaky.Var("occupied", 0d);

        Assert.Throws<InitialisationFailure>(() => leaky.Type("Broken", ("leaked", 0d), ("leaked", 1d)));
        Assert.Equal(1, leaky.Declared);

        // and the module-level name it used to collide with is untouched either way
        leaky.Type("Fixed", ("leaked", 0d), ("occupied", 0d));

        Assert.Equal(3, leaky.Declared);
    }

    [Fact(DisplayName = "and one member declared twice in the same type is refused")]
    public void AndOneMemberDeclaredTwiceInTheSameTypeIsRefused()
        // Not reachable through «Unique», because the first declaration has not
        // happened yet when the preflight asks.
        => Assert.Contains("twice",
                           Assert.Throws<InitialisationFailure>(
                               () => new Graph().Type("Box", ("cash", 0d), ("cash", 1d))).Message);

    [Fact(DisplayName = "removing one twice says so rather than removing another")]
    public void RemovingOneTwiceSaysSoRatherThanRemovingAnother()
    {
        var graph = Boxes(2, out var boxes);

        graph.Remove(boxes[0]);

        // Within the round it is still there, so removing it again is the same
        // removal and not a mistake — «leaving» is a set. It is the NEXT round
        // that can tell, because by then the handle names nothing.
        graph.Remove(boxes[0]);
        graph.Step();

        // The call would otherwise release a slot that belongs to whatever took
        // it, and swap-with-last would move a live instance out from under its
        // own handle.
        Assert.Contains("already removed",
                        Assert.Throws<InitialisationFailure>(() => graph.Remove(boxes[0])).Message);
    }

    [Theory(DisplayName = "and writing through a handle that does not fit is refused")]
    [InlineData(true)]
    [InlineData(false)]
    public void AndWritingThroughAHandleThatDoesNotFitIsRefused(bool removed)
    {
        Graph graph = new();
        graph.Type("Box", ("cash", 0d));
        graph.Type("Crate", ("weight", 0d));

        var box = graph.Create("Box");

        if (removed)
        {
            graph.Remove(box);
            graph.Step();
        }

        // A write cannot answer with an error the way a read can, so it refuses
        // where a read reports. Same two questions either way: is this member
        // this type's, and is this handle still anybody's.
        Assert.Throws<PurityViolation>(() => graph.Write(removed ? "cash" : "weight", box, 1d));
    }

    [Fact(DisplayName = "a removal is a write, so a reader learns of it at the round boundary")]
    public void ARemovalIsAWriteSoAReaderLearnsOfItAtTheRoundBoundary()
    {
        // Found by audit, adjudicated by the designer, and the timing is the
        // part worth pinning. Removal used to compact the arrays on the spot and
        // advance nothing, so a derived reader stayed cached at its last value —
        // the stable handle held for a direct read and not for one through a
        // «let», and the wrong answer was permanent unless something else
        // happened to dirty the cell.
        //
        // But reading «0» in the SAME round is correct and must stay correct. A
        // removal that landed the instant it was called would make a step
        // order-dependent: a «when» that removes and one that reads would give
        // two answers depending on which was declared first, which is what
        // buffered writes exist to prevent. So the defect is the round after.
        var graph = Boxes(2, out var boxes);

        graph.Let("observed", scope => scope.Read("cash", boxes[0]));
        graph.Prime();

        Assert.Equal(0d, graph.Read("observed"));

        graph.Remove(boxes[0]);

        Assert.Equal(0d, graph.Read("observed"));

        graph.Step();

        Assert.IsType<Error>(graph.Read("observed"));
        Assert.IsType<Error>(graph.Read("cash", boxes[0]));
    }

    [Fact(DisplayName = "and a write staged for it goes with it, rather than landing on its neighbour")]
    public void AndAWriteStagedForItGoesWithItRatherThanLandingOnItsNeighbour()
    {
        // Found by audit. The write staged a dense INDEX, and removal is
        // swap-with-last, so the survivor moved into the removed instance's slot
        // and collected a value written for somebody else — the identity failure
        // the generational handle exists to prevent, reintroduced by turning the
        // handle into a location before the write had settled.
        var graph = Boxes(2, out var boxes);

        graph.Write("cash", boxes[0], 7d);
        graph.Remove(boxes[0]);
        graph.Step();

        Assert.Equal(0d, graph.Read("cash", boxes[1]));

        // and the mirror, which used to index past the end of a compacted array
        // and take the step out with it
        var mirror = Boxes(2, out var pair);

        mirror.Write("cash", pair[1], 7d);
        mirror.Remove(pair[0]);
        mirror.Step();

        Assert.Equal(7d, mirror.Read("cash", pair[1]));
    }

    [Fact(DisplayName = "and a cell that stays stale stops waking its readers")]
    public void AndACellThatStaysStaleStopsWakingItsReaders()
    {
        // Cutoff compares a recompute with the cached value, and an error was
        // compared by reference — so a reader of a removed instance produced a
        // NEW error every round, advanced the clock every round, and the graph
        // never went quiet. Exactly what cutoff exists to prevent, arriving by a
        // different door.
        var graph = Boxes(2, out var boxes);
        var evaluated = 0;

        graph.Let("observed", scope =>
        {
            ++evaluated;
            return scope.Read("cash", boxes[0]);
        });

        graph.Prime();
        graph.Read("observed");

        graph.Remove(boxes[0]);
        graph.Step();

        Assert.IsType<Error>(graph.Read("observed"));

        var settled = evaluated;

        graph.Step();
        graph.Step();

        Assert.IsType<Error>(graph.Read("observed"));
        Assert.Equal(settled, evaluated);
    }

    [Fact(DisplayName = "two instances written in one step both land")]
    public void TwoInstancesWrittenInOneStepBothLand()
    {
        // A member is ONE node holding N values, so two instances written in one
        // step are two writes to the same node. Staged per cell, last-write-wins
        // would keep one of them — which is the collision that cost a run when
        // chain counters shared a node, one level down.
        var graph = Boxes(3, out var boxes);

        graph.Write("cash", boxes[0], 1d);
        graph.Write("cash", boxes[2], 3d);
        graph.Step();

        Assert.Equal([1d, 0d, 3d], boxes.Select(box => graph.Read("cash", box)));
    }

    [Fact(DisplayName = "a member wakes what reads it, once for all of them")]
    public void AMemberWakesWhatReadsItOnceForAllOfThem()
    {
        var graph = Boxes(3, out var boxes);
        var evaluated = 0;

        graph.Let("total", scope =>
        {
            ++evaluated;
            return boxes.Sum(box => (double)scope.Read("cash", box));
        });

        graph.Prime();

        Assert.Equal(0d, graph.Read("total"));

        var before = evaluated;

        graph.Write("cash", boxes[0], 10d);
        graph.Write("cash", boxes[1], 20d);
        graph.Step();

        Assert.Equal(30d, graph.Read("total"));
        Assert.Equal(before + 1, evaluated);
    }

    [Fact(DisplayName = "and a write that changes nothing wakes nobody")]
    public void AndAWriteThatChangesNothingWakesNobody()
    {
        var graph = Boxes(2, out var boxes);
        var evaluated = 0;

        graph.Let("total", scope =>
        {
            ++evaluated;
            return boxes.Sum(box => (double)scope.Read("cash", box));
        });

        graph.Prime();

        Assert.Equal(0d, graph.Read("total"));

        var before = evaluated;

        graph.Write("cash", boxes[0], 0d);
        graph.Step();

        Assert.Equal(0d, graph.Read("total"));
        Assert.Equal(before, evaluated);
    }
}
