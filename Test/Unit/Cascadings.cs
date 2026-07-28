// Copyright © 2026 Eric Budai

using Ronin.Runtime;

namespace Unit;

/// <summary>
///     Tier one of three: a <c>when</c> cycle found at declaration, before
///     anything runs.
/// </summary>
[Trait(nameof(Cascades), null)]
public class Cascadings
{
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

    private static IReadOnlyDictionary<string, IReadOnlyCollection<Write>> Writing(
        params (string When, string[] Cells)[] whens)
        => whens.ToDictionary(
               entry => entry.When,
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

        Assert.Equal(
            "«game state» is written by 2 whens: «when player dies», «when timer expires». " +
            "Whens fire in one round with no order between them, so one write would land and " +
            "the other vanish. Derive the value instead, with a let that reads both conditions.",
            complaint);
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
        Dictionary<string, IReadOnlyCollection<Write>> whens = new()
        {
            ["when player dies"] = [new Write("log", "when player dies")],
            ["when timer expires"] = [new Write("log", "when timer expires")],
        };

        Assert.Single(Cascades.Writers(whens));

        // and one when reaching a cell by two routes is charged once
        Dictionary<string, IReadOnlyCollection<Write>> twice = new()
        {
            ["when player dies"] = [new Write("log", "when player dies"), new Write("log", "when player dies")],
        };

        Assert.Empty(Cascades.Writers(twice));
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
        Assert.Empty(Cascades.Diagnose(whens));
    }

    [Fact(DisplayName = "the diagnosis names the whole ring")]
    public void TheDiagnosisNamesTheWholeRing()
    {
        var complaint = Assert.Single(Cascades.Diagnose(new Dictionary<string, Effects>
        {
            ["ping"] = Does(["pong count"], ["ping count"]),
            ["pong"] = Does(["ping count"], ["pong count"]),
        }));

        Assert.Equal(
            "«ping» → «pong» → «ping» is a cycle: each writes something the next reads, so " +
            "firing one schedules the next. Stop one of them writing what the ring reads, " +
            "or declare feedback on every when in the ring.",
            complaint);
    }
}
