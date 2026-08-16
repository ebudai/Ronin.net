# Postfix patterns — measured against `Resolver.cs`, and §8 is missing two items

> **Ledger** — `[R]` Postfix patterns — measured against `Resolver.cs`, and §8 is missing two items
> answered by: POSTFIXDIAGNOSIS
> measured at: 6111c17

Answering `POSTFIXPATTERNS.md`. Nothing was implemented: the document says twice
that nothing should be, and §8's three preconditions are unmet. What follows is
verification, done with a temporary experiment that was removed before any gate
ran.

**Headline: §4's property does not reproduce on the shipped resolver.** The one
composition the whole reversal rests on resolved to a single derivation and
chose one reading, silently. That may be my wiring rather than the algorithm,
and distinguishing the two is exactly the work §8(a) asks for — but it has to be
done on `Resolver.cs` and not on `dp_resolver.py`, and §8 does not say so.

## 1. Where the ban actually lives — three layers, not one

| layer | what it does |
|---|---|
| `Pattern..ctor` (`Resolver.cs:725-729`) | **throws** `ArgumentException` — a leading-hole pattern cannot exist as a value |
| `Declarations` / `LeadingHole` finding | the explicit rule `R6ANDINFIX.md` §1 asked for |
| `Resolver` (`:108-116, :204-211`) | indexes patterns by `Segments[0]` and returns early unless the span starts with a word keying that index |

Only the middle one is a rule. The other two are structure, and the third is the
item §8 omits — see §3 below.

**The refuted sentence is in the compiler.** `Resolver.cs:725-727` carries the
`ronin_grammar_probe.py` claim verbatim:

> A pattern beginning with a hole is left recursive: resolving an atom at
> position p would require resolving an atom at position p. Infix must be
> symbolic; word patterns must be prefix.

§9 asks for the probe's copy to be corrected in place. This copy is load-bearing
prose in the shipped compiler and reads as a language constraint, so it should
be corrected in the same pass — whichever way the decision goes, since it is
wrong either way.

## 2. The measurement

The experiment lifted exactly two things: the constructor throw, and the
first-word index (leading-hole patterns collected separately and offered on
every span). `Match` was **not** touched — it already handles a hole at any
segment by trying splits, which is why this was cheap enough to be worth doing.

Patterns `sorted (_)` and `(_) reversed`, name `xs`:

| source | kind | tree |
|---|---|---|
| `xs reversed` | Resolved | `[(_) reversed](«xs»)` |
| `sorted xs` | Resolved | `[sorted (_)](«xs»)` |
| **`sorted xs reversed`** | **Resolved** | **`[(_) reversed]([sorted (_)](«xs»))`** |
| `(sorted xs) reversed` | Resolved | as written, cost 4 |
| `sorted (xs reversed)` | Resolved | as written, cost 4 |
| `xs reversed sorted` | Resolved | `[(_) sorted]([(_) reversed](«xs»))` |
| `xs sorted reversed` | Resolved | `[(_) reversed]([(_) sorted](«xs»))` |

The third row is the one that matters. §4 predicts a tie at cost 3 with two
derivations; the shipped resolver returned **one** derivation and picked
`(sorted xs) reversed`. Ambiguity here is decided by derivation count —
`Resolver.cs:152`, `best.Count > 1` — so this is not a rendering artefact. The
other reading was never offered.

That is silent capture: a wrong reading available to a reader, not reported, and
not repaired by R5, because R5's reservation of `reversed` does nothing about a
*grouping* choice between two legal readings.

Both postfixes composing (`xs reversed sorted`) is unambiguous and reads
left-to-right, which supports §6's "a single postfix use never ties" and extends
it to postfix∘postfix.

### What this does not establish

I cannot yet say whether the missing derivation is the DP's structure or my
wiring. The candidate cause is the open-ended trailing argument: `sorted (_)` is
open-ended and its trailing argument is parsed at the pattern's own binding
power through `open`, while a non-open-ended leading-hole call is offered into
the closed cell. If those two never meet at a compatible minimum, the reading is
unreachable by construction rather than by omission.

Resolving that is a day's work in the DP and it is the *first* thing §8(a)
should ask for, because if the tie is unreachable then the reversal's safety
argument has no support on the real implementation, and if it is reachable then
the DP has to be changed to reach it — which is a cost §7 does not list.

## 3. Two items §8 is missing

**d. The first-word index has no key for a leading-hole pattern.** `anchored` is
built on `pattern.Segments[0]`, and `Expression` returns immediately unless the
span begins with a word that keys it. A postfix pattern has no such key, so it
must either be tried on every span — which is what my experiment did, and it is
the O(patterns × spans) walk the index was introduced to remove — or indexed by
something else, its last word being the obvious candidate.

This is not a detail. `ResolverCost.cs` names that index as one of **three**
optimisations its allocation ceiling exists to protect, alongside the triangular
table and lazy collection. The ceiling is currently 26 MB against 21.1 measured,
so there is not much room to give back.

**e. The tie property must be re-measured on `Resolver.cs`.** §4, §6 and the
0.04% all come from `dp_resolver.py`. `Resolver.cs` ports it, but §2's whole
lesson is that a property of one instrument was written up as a property of the
language — and the measurement above is the same shape of divergence in the
other direction. The numbers that decide this should come from the compiler.

## 4. On §8(c), which I could not do

The tie rate over real programs is unmeasurable today because there are no real
programs: there is no corpus of Ronin source, and the language cannot yet
express most of what a corpus would contain. The honest substitute is the test
suite's own patterns and names, which is a few dozen and not a distribution.

If that number is genuinely the one that decides the bracketing burden, then
this decision is blocked on having programs, not on analysis — which is worth
saying plainly rather than leaving as an open item that looks actionable.

## 5. What I would do next, if asked

1. Settle §8(a) by determining whether the shipped DP can offer both derivations
   at all — that is a measurement, not a design, and everything else waits on
   it.
2. Correct the left-recursion comment in `Resolver.cs` regardless of the
   outcome. It is wrong now.
3. Only then look at R6's replacement, which is the part with no rule proposed
   and which I would not guess at either.
