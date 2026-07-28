// Copyright © 2026 Eric Budai

using Ronin.Compiler;
using Ronin.Runtime;

namespace Unit;

/// <summary>
///     Tier one of three: a <c>when</c> cycle found at declaration, before
///     anything runs.
/// </summary>
[Trait(nameof(Cascades), null)]
public class Cascadings
{
    /// <summary>
    ///     Spans for a rule test, which reads names and never their positions.
    /// </summary>
    private static readonly SourceText Nowhere = new(string.Empty);

    private static Triggering When(string name) => new(name, Nowhere.Span(0, 0));

    private static Effects Does(string[] reads, string[] writes, bool feedback = false)
        => new(new HashSet<string>(reads), new HashSet<string>(writes), feedback);

    private static Dictionary<string, Effects> Sample(bool settling = false) => new()
    {
        ["temp moved"] = Does(["temp"], ["temp"]),
        ["on damage"] = Does(["health"], ["is alive", "log"]),
        ["on death"] = Does(["is alive"], ["respawn timer"]),
        ["ping"] = Does(["pong count"], ["ping count"]),
        ["pong"] = Does(["ping count"], ["pong count"]),
        ["on respawn"] = Does(["respawn timer"], ["health"]),
        ["layout settle"] = Does(["box sizes"], ["box sizes"], feedback: settling),
    };

    private static Dictionary<Triggering, Effects> Triggered(bool settling = false)
        => Sample(settling).ToDictionary(entry => When(entry.Key), entry => entry.Value);

    [Fact(DisplayName = "every ring is found, whole, before anything runs")]
    public void EveryRingIsFoundWholeBeforeAnythingRuns()
    {
        // the three-hop ring is the one nobody spots by reading, and naming a
        // single participant would make it unreadable
        Assert.Equal(
            [
                ["layout settle", "layout settle"],
                ["on damage", "on death", "on respawn", "on damage"],
                ["ping", "pong", "ping"],
                ["temp moved", "temp moved"],
            ],
            Cascades.Cycles(Sample()));
    }

    [Fact(DisplayName = "a ring everyone opted into is the feature")]
    public void ARingEveryoneOptedIntoIsTheFeature()
    {
        // constraint relaxation writes the sizes it reads until they stop moving;
        // banning that costs layout solving, physics settling, and every state
        // machine that transitions on its own state
        var rings = Cascades.Cycles(Sample(settling: true));

        Assert.DoesNotContain(rings, ring => ring.Contains("layout settle"));
        Assert.Equal(3, rings.Count);
    }

    [Fact(DisplayName = "a ring one participant did not opt into is still reported")]
    public void ARingOneParticipantDidNotOptIntoIsStillReported()
    {
        // joining a feedback ring has to be deliberate for everyone in it, or the
        // participant who did not agree is the one debugging it
        Dictionary<string, Effects> whens = new()
        {
            ["settling"] = Does(["size"], ["hint"], feedback: true),
            ["unwitting"] = Does(["hint"], ["size"]),
        };

        Assert.Single(Cascades.Cycles(whens));

        // and the edit the message names — feedback on every when in the ring —
        // is the one that actually clears it
        whens["unwitting"] = Does(["hint"], ["size"], feedback: true);
        Assert.Empty(Cascades.Cycles(whens));
    }

    private static IReadOnlyDictionary<Triggering, IReadOnlyCollection<Write>> Writing(
        params (string When, string[] Cells)[] whens)
        => whens.ToDictionary(
               entry => When(entry.When),
               entry => (IReadOnlyCollection<Write>)[.. entry.Cells.Select(cell => new Write(cell, entry.When))]);

    [Fact(DisplayName = "two whens writing one cell is a declaration error")]
    public void TwoWhensWritingOneCellIsADeclarationError()
    {
        // They fire in one round with no order between them, so one write lands
        // and the other vanishes — and the program looks fine. Declaration order
        // would make that deterministic and still silent, which is worse.
        var complaint = Assert.Single(Cascades.Writers(Writing(
            ("when player dies", ["game state", "log"]),
            ("when timer expires", ["game state"]),
            ("when score changes", ["score display"]))));

        Assert.Equal(FindingKind.ManyWriters, complaint.Kind);
        var writers = Assert.IsType<ManyWriters>(complaint);

        Assert.Equal("game state", writers.Cell);
        Assert.Equal(2, writers.Writers.Count);

        // one span per writer, so both sites are named
        Assert.Single(complaint.Related);
    }

    [Fact(DisplayName = "one when writing many cells is fine")]
    public void OneWhenWritingManyCellsIsFine()
    {
        Assert.Empty(Cascades.Writers(Writing(
            ("when player dies", ["game state", "log", "respawn timer"]))));
    }

    [Fact(DisplayName = "a write is charged to the when, not the writer")]
    public void AWriteIsChargedToTheWhenNotTheWriter()
    {
        // A write reached through a call belongs to the when that made the call,
        // because that is what the programmer can move. Two whens calling one
        // shared function are still two writers of its cell.
        Dictionary<Triggering, IReadOnlyCollection<Write>> whens = new()
        {
            [When("when player dies")] = [new Write("log", "when player dies")],
            [When("when timer expires")] = [new Write("log", "when timer expires")],
        };

        Assert.Single(Cascades.Writers(whens));

        // and one when reaching a cell by two routes is charged once
        Dictionary<Triggering, IReadOnlyCollection<Write>> twice = new()
        {
            [When("when player dies")] = [new Write("log", "when player dies"), new Write("log", "when player dies")],
        };

        Assert.Empty(Cascades.Writers(twice));
    }

    [Fact(DisplayName = "a ring reachable only past a settled participant is still found")]
    public void ARingReachableOnlyPastASettledParticipantIsStillFound()
    {
        // The bypass a back-edge walk leaves open. «a → b → a» is found first and
        // allowed, because both declared feedback; the walk then settles «b» and
        // never revisits it, so «a → c → b → a» is never seen at all — and «c»
        // sits in a feedback ring it never opted into with nothing said.
        //
        // Legality belongs to the component, not to the individual rings: every
        // member of one is in a ring with every other, so «c» being in it is the
        // whole question.
        Dictionary<string, Effects> whens = new()
        {
            ["a"] = Does(["ra"], ["rb", "rc"], feedback: true),
            ["b"] = Does(["rb"], ["ra"], feedback: true),
            ["c"] = Does(["rc"], ["rb"]),
        };

        // named from «c», because that is the one declaration that clears it
        Assert.Equal([["c", "b", "a", "c"]], Cascades.Cycles(whens));

        // and declaring feedback on it is the edit the message asks for
        whens["c"] = Does(["rc"], ["rb"], feedback: true);
        Assert.Empty(Cascades.Cycles(whens));
    }

    [Fact(DisplayName = "reaching one when by two routes is not a ring")]
    public void ReachingOneWhenByTwoRoutesIsNotARing()
    {
        // A diamond has an edge into something already finished with, which is
        // the case a back-edge walk cannot tell from an edge back into the walk
        // itself. Nothing here is cyclic.
        Assert.Empty(Cascades.Cycles(new Dictionary<string, Effects>
        {
            ["source"] = Does(["in"], ["left", "right"]),
            ["long way"] = Does(["right"], ["left"]),
            ["sink"] = Does(["left"], ["out"]),
        }));
    }

    [Fact(DisplayName = "a very long chain of whens is analysed, not crashed on")]
    public void AVeryLongChainOfWhensIsAnalysedNotCrashedOn()
    {
        // The depth here is the program's rather than the algorithm's, and both
        // the component walk and the initialisation ordering recursed down it —
        // so a long enough chain ended the process with a StackOverflowException,
        // the one failure a diagnostic pass cannot report because it cannot be
        // caught.
        Dictionary<string, Effects> whens = [];
        Dictionary<string, IReadOnlySet<string>> initialisers = [];

        for (var hop = 0; hop < 50_000; ++hop)
        {
            whens[$"when {hop}"] = Does([$"cell {hop}"], [$"cell {hop + 1}"]);
            initialisers[$"value {hop}"] = new HashSet<string>(hop is 0 ? [] : [$"value {hop - 1}"]);
        }

        // a chain is not a ring, however long it is
        Assert.Empty(Cascades.Cycles(whens));

        Assert.True(Initialisation.TryOrder(initialisers, out var order));
        Assert.Equal("value 0", order[0]);
        Assert.Equal("value 49999", order[^1]);

        // and closing it into one ring is still found
        whens["when 49999"] = Does(["cell 49999"], ["cell 0"]);
        Assert.Single(Cascades.Cycles(whens));
    }

    [Fact(DisplayName = "an acyclic set reports nothing")]
    public void AnAcyclicSetReportsNothing()
    {
        Dictionary<string, Effects> whens = new()
        {
            ["first"] = Does(["a"], ["b"]),
            ["second"] = Does(["b"], ["c"]),
        };

        Assert.Empty(Cascades.Cycles(whens));
        Assert.Empty(Cascades.Diagnose(whens.ToDictionary(entry => When(entry.Key), entry => entry.Value)));
    }

    [Fact(DisplayName = "the diagnosis names the whole ring")]
    public void TheDiagnosisNamesTheWholeRing()
    {
        var complaint = Assert.Single(Cascades.Diagnose(new Dictionary<Triggering, Effects>
        {
            [When("ping")] = Does(["pong count"], ["ping count"]),
            [When("pong")] = Does(["ping count"], ["pong count"]),
        }));

        Assert.Equal(FindingKind.CascadeRing, complaint.Kind);
        Assert.Equal("ping» → «pong» → «ping", Assert.IsType<CascadeRing>(complaint).Ring);

        // «ping» opens and closes the ring, so only «pong» is named beside it
        Assert.Single(complaint.Related);
    }
}
