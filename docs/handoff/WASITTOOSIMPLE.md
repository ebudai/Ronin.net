# It was not too simple — and the reason it works is a property nobody has written down

You guessed I would come up with an easy counterexample. I went looking for one
and **failed**, twice, in opposite directions. The measurement says your original
algorithm is affordable, and the reason is structural rather than lucky — which
means it is something to protect deliberately rather than to be pleased about.

Two real criticisms survive, and neither is about tractability.

---

## 1. The enumeration is cheap, and I was wrong to think otherwise

I expected "find all the possibilities" to blow up with statement length. It does
not. Searching 4000 random statements per length, with **every substring in the
program declared as a name** — a hostility no real module reaches:

```
   words   max derivations   worst example
       6                 5   send print to print to item
       9                 8   send sum of to to item to of to
      12                18   item print of of print item a of of print sum sum
```

Eighteen, at twelve words, in the worst case I could find. Not exponential —
barely superlinear.

*(My first run of this probe was also wrong, in the other direction: it counted
zero for everything, because I had built statements no pattern could cover. Both
errors are in the file.)*

### Why it works, and this is the part worth keeping

**Ronin has no juxtaposition.** `a b` is not an application, not a product, not
anything. So an unbracketed argument must be exactly **one** expression covering
exactly that span — and the number of ways to do that is bounded by how many
names and patterns happen to fit that span, not by how many ways the words could
be cut up.

The combinatorial explosion everyone expects from spaces-in-names comes from
grammars where **adjacency composes**. Haskell-style application, or an implicit
product, would make this exponential, because every gap between words becomes a
choice. Ronin has no gaps to choose.

> **This belongs in the spec as a constraint, not an observation.** Adding an
> adjacency operator later — application, implicit multiplication, string
> concatenation by juxtaposition — would take the resolver from near-linear to
> exponential, and nothing in the grammar would announce it. It is the single
> load-bearing property of the whole scheme and it is currently implicit.

So: no, you were not glossing over an unsolved problem here. You were relying on
something true that had not been named.

### And it explains what this week's rules are actually for

The derivations that *do* exist all come from one place:

```
  item print of print of a of c       8   a pattern anchor usable as a name
  send sum of to to item to of to     8   a glue word usable as a name
```

A word that is **structure in one reading and spelling in another**. Which is
exactly what R5′, R6b and R7b regulate. So those rules are not complexity piled
on top of the design — they are what keeps this count at **one** for code anyone
would actually write. They look like accumulating fuss; they are the thing that
makes the simple algorithm hold.

## 2. The type checker cannot break the ties we actually have

This is the criticism that survives. Of the six ties measured this session:

```
  statement                reading A  reading B  types decide?
  x is a number            truth      truth      no
  send to to to to         nothing    nothing    no
  sum of all things        number     number     no
  send time to live        nothing    nothing    no
  x is not x               truth      truth      no
  sorted xs reversed       list       ERROR      YES
```

Five of six have the **same type on both sides**. A type filter cannot touch
them. The one it does break is the postfix case — the example you raised months
ago, and you were right about that one.

So types and minimum lookup are **complementary, not alternative**:

| | decides |
|---|---|
| minimum lookup | **segmentation** — where a name starts and stops |
| types | **applicability** — whether an operand fits |

Types alone leave every segmentation tie standing. Lookup alone leaves the
postfix family standing. What survives both is what genuinely needs a bracket —
which is the design you described, with one more filter in it than you
remembered.

## 3. "Every word is its own scope" — and this is where it touches the open question

The trie model is a good representation and it has one hidden requirement worth
surfacing, because it is the thing currently blocking three slices.

A trie keyed by word only works if there **is** a first word to key on. That is
R6 — anchor runs prefix-free — and it is exactly what refuses `(_) is (_)`. So
your original model and the leading-holes question are the **same question**:

- keep the trie model → patterns must be anchor-first → `is` and `otherwise` live
  in the operator table → B₁ gets built;
- admit leading holes → the trie stops being sufficient → you need the chart,
  which is what is already there → B₁ gets deleted.

You have effectively already chosen the second by having a chart resolver. The
open decision is whether to notice.

## 4. One cost that is real, though small at these numbers

Type-checking a candidate is not free, because **in Ronin the parse decides which
names exist**. `for each item in items { … }` declares `item` inside the block —
a different parse gives a different symbol table for the body. So each candidate
needs its own speculative scope before it can be typed.

At eighteen candidates that is nothing. It is worth knowing it is per-candidate
rather than per-statement, because it is the term that would bite if the
juxtaposition constraint above were ever relaxed.

## 5. On downstream statements — agreed, with one refinement

You are right that adding a declaration above changing the meaning below is not
unusual, and dynamic languages do worse. One difference is worth naming, because
it is the gap your own colouring proposal closes:

In Python, a later definition changes **what runs**. The text still *reads* the
same, and a person can follow it without knowing the binding. Here it changes
**what the text means** — which phrase is one name. That is a reading problem
rather than a runtime problem, and reading problems are fixed by showing the
reader, not by rules.

So: colour the resolved name's span as one unit, and **hover to show the implicit
brackets** — I think that second one is the better of the two ideas. It answers
"what did the compiler think I wrote" directly, which is the only question a
person actually has when a candy grammar surprises them.

And the machinery exists: the repair search that enumerates single-bracket
insertions is already in the grammar probe, and it is what produces the
suggestions for a genuine ambiguity. Hover-to-reveal is the same function run on
the *winning* parse instead of on the failures.

## 6. Summary

| | |
|---|---|
| is the enumeration too slow | **no** — 18 derivations worst case at 12 words, under maximal hostility. I was wrong |
| why | **no juxtaposition** — adjacency does not compose, so there are no cut points to choose. Put it in the spec as a constraint |
| does the type checker break the ties | **mostly no** — 5 of 6 measured ties have the same type on both sides |
| so what breaks them | minimum lookup for segmentation, types for applicability, brackets for what survives both |
| were you glossing over something | not here. The rules this week are what keep the count at one, not extra complexity |
| "every word is a scope" | requires anchor-first patterns, i.e. R6 — the same decision as leading holes |
| colouring / hover | **yes**, and hover-shows-implicit-brackets is the stronger of the two |

Probe: `parse_count.py`.
