# Leading holes — settled, and the rule wants narrowing in exactly one place

Answering the open question from `4829af8`. `fuzz_leading.py`; run it, since the
missing dependency is now shipped.

---

## 0. The reproducibility hit is fair, and taken

`ronin_grammar_probe.py` was never in the handoff folder, so none of the three
files could run and the 45,131,520 figure was quoted rather than reproduced.
That is a bad look for a note whose subject was numbers quoted without their
scope. It is shipped now — it is the only missing dependency; the three files
run clean against it in an otherwise empty directory.

---

## 1. Result

Three admission policies for a pattern beginning with a hole, everything else
held constant:

| policy | leading holes admitted | patterns | pairs | resolutions | ties |
|---|---|---|---|---|---|
| `strict` | none — the rule as implemented | 78 | — | baseline | — |
| `bracket` | `{_}` only | 90 | 990 | 10,490,112 | **0** |
| `loose` | `{_}` and `<_>` | 102 | 2,124 | 22,731,264 | **1,134** |

**So yes — the rule is stricter than it needs to be, and in exactly one place.
Admit a leading `{_}`. Keep refusing a leading `<_>` and a leading `(_)`.**

## 2. Why `<_>` fails where `{_}` does not

The counterexample class is small and immediately convincing:

```
patterns   «<_> b»  and  «a (_)»
names      a, b
statement  a b
    [a «b»]        2 lookups
    [«a» b]        2 lookups
```

A leading `<_>` matches *any* single word, so it competes with every
word-anchored pattern at the same position. That is precisely the
pattern-prefix ambiguity R6 exists to prevent — `b (_)` beside `b b (_)`, in a
new costume.

A leading `{_}` cannot compete, because it must begin with `(`, and no
word-anchored pattern can start there.

### The distinction worth writing into the rule

The useful generalisation is **not** "does it start with a word". It is:

> A leading segment is admissible when it is determinate in **identity** as
> well as in extent.

| segment | extent determinate? | identity determinate? | legal leading? |
|---|---|---|---|
| word | yes | yes — that word | **yes** |
| `{_}` bracketed | yes — bracket matching | yes — must be `(` | **yes** |
| `<_>` one token | yes — one token | **no** — any word | no |
| `(_)` free | no | no | no |

`<_>` is the interesting row: it is determinate in extent, which is why it works
perfectly well in *interior* position — that is what buys back `in`, and the
earlier run found 0 ties for it there. It fails only in leading position, where
identity is what matters because nothing has anchored the match yet.

## 3. What to change

**R6, restated.** Compare patterns by their *determinate prefix* — the leading
run of segments up to the first free `(_)`. Words, `{_}` and `<_>` are all
determinate in extent, so all three belong in the run. Require determinate
prefixes to be prefix-free. This is identical to the current anchor-run rule
whenever no `{_}`/`<_>` is present, so nothing existing changes.

**`LeadingHole`, narrowed.** Refuse a leading free `(_)` and a leading `<_>`.
Admit a leading `{_}`. Two findings rather than one, because the reasons differ
and the advice does:

> A pattern may not begin with «(_)»: its extent is unmarked, so nothing
> anchors the match. Lead with a word, bracket the argument as «{_}», or
> declare a symbolic operator.

> A pattern may not begin with «<_>»: a single-token hole matches any word, so
> it collides with every pattern that begins with one. Lead with a word or
> bracket the argument.

The second message should carry the counterexample shape — it is the sort of
rule that reads as arbitrary until someone sees `a b` resolving two ways.

## 4. What this unblocks, and what it does not

`§6` of `FUZZ-BRACKETS.md` asked for leading-`{_}` to be settled before the
`THOLE` change that unreserves `in`. It is settled, and the answer is
independent of the `in` question — leading position and interior position turn
out to be governed by different properties, which is why one is safe and the
other is not.

So **step 5 is unblocked.** `docs/reserved-words.txt` goes to `## RESERVED (0)`
as a one-line diff.

Two things still outside every run so far, neither blocking:

- names longer than two words;
- pattern pairs at 3-unit statements — the 2-unit runs above are what fits the
  time budget here, and the 3-unit single-pattern runs were clean. A machine
  that can sit on it for an hour would close both.

## 5. On the invariant

Your framing of it is better than mine, and the sentence worth keeping is
yours: *widening what may be part of a name leaves every resolution unique and
wrong, and no fuzzer reports it, because uniqueness is exactly what it checks.*

`NameInvariant` is the right test and `Resolver.AllWords` is the right place for
the reasoning to live. The one thing I would add: the test should assert the
negative shape explicitly — that the swallowing reading is **absent from the
parse set**, not merely more expensive than the winner. Those are different
assertions, and only the first one fails loudly if someone later lets a name
span a bracket.
