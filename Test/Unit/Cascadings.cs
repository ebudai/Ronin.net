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
