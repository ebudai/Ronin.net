// Copyright © 2026 Eric Budai

using Ronin.Compiler;
using Scope = Ronin.Grammar.Scope;
using When = Ronin.Lexicon.When;

namespace Unit;

/// <summary>
///     A <c>when</c> is named by its trigger, which needs no syntax at all.
/// </summary>
[Trait(nameof(Triggers), null)]
public class TriggerNames
{
    private static string Trigger(string source)
    {
        Lexer lexer = new(source);
        Parser parser = new(lexer.Lex());

        return Scope.Parse(ref parser) switch
        {
            Scope.Reactive reactive => reactive.Trigger,

            // Conditional<T>.Parse builds the generic base rather than the
            // ConditionalReactive alias, so the closed type is what identifies it
            Scope.Conditional<When> conditional => conditional.Trigger,
            var other => throw new Xunit.Sdk.XunitException($"not a when: {other?.GetType().Name ?? "null"}"),
        };
    }

    [Fact(DisplayName = "a when is named by what it triggers on")]
    public void AWhenIsNamedByWhatItTriggersOn()
    {
        // «on damage» asks the reader to trust that damage is what changes
        // health; this says so, in the programmer's own words.
        Assert.Equal("when health drops", Trigger("when health drops { x = 1; }"));

        // the mode is in the source, so it is in the name for free — these are
        // different events on the same value and they read differently
        Assert.Equal("when temperature > 6", Trigger("when temperature > 6 { x = 1; }"));
        Assert.Equal("when changing temperature", Trigger("when changing temperature { x = 1; }"));
    }

    [Fact(DisplayName = "the name is canonical, not the source verbatim")]
    public void TheNameIsCanonicalNotTheSourceVerbatim()
    {
        // one spelling per meaning, still greppable
        Assert.Equal("when x > 6", Trigger("when x>6 { y = 1; }"));
        Assert.Equal("when x > 6", Trigger("when    x   >    6 { y = 1; }"));
    }

    [Fact(DisplayName = "brackets and separators hug their contents")]
    public void BracketsAndSeparatorsHugTheirContents()
    {
        Assert.Equal("when distance between (a, b) > 6",
                     Trigger("when distance between (a, b)>6 { y = 1; }"));
    }

    [Fact(DisplayName = "a long trigger keeps both of its ends")]
    public void ALongTriggerKeepsBothOfItsEnds()
    {
        // the ends are the informative parts and the middle is the conjunction
        const string full = "player health is below critical threshold and shield is down " +
                            "and revive charges remaining is zero";

        Assert.Equal("player health is below  ... arges remaining is zero", Triggers.Elide(full));

        // and anything that fits is left alone
        Assert.Equal("health drops", Triggers.Elide("health drops"));
    }

    [Fact(DisplayName = "identical triggers take an ordinal")]
    public void IdenticalTriggersTakeAnOrdinal()
    {
        // legal and rare: scope qualifies them first where there is one, and the
        // ordinal is the last resort
        Assert.Equal(
            ["when health changes", "when shield drops", "when health changes #2"],
            Triggers.Distinct(["when health changes", "when shield drops", "when health changes"]));
    }
}
