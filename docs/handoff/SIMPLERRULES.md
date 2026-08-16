# Yes — and it simplifies more than you are asking about

> **Ledger** — `[V]` Yes — and it simplifies more than you are asking about
> supersedes: none
> superseded by: none

Two answers to "are you agreeing": **yes on the algorithm**, with one
disagreement retained about the tie-breaker. And a correction to something I told
you yesterday, which was wrong for the reason you just gave.

Then the real answer to "does this simplify the rules": **it could delete three
of them and minimum lookup with them**, and the price is measurable.

---

## 1. Correction: the trie on *parts* does not require R6

Yesterday I said your "every word is a scope" model needs a first word to key on,
and therefore requires anchor-first patterns — i.e. that your model and R6 were
the same commitment.

That was wrong, and the reason is exactly your clarification. If the edges are
**parts** — word *or* param block — then a hole-labelled edge is fine. It just
matches **nondeterministically in extent**: the descent has to try every k.

And when you memoise that descent over (node, position)… you have the chart
resolver. `match(pat, si, i, j)` in every probe I have written *is* a walk down
your trie with the extent search memoised.

So they were never rival designs. **The DP is your trie, made efficient**, and
your model imposes no constraint on leading holes at all. That removes one of the
arguments I gave for keeping R6, and it strengthens the case I was already
building for admitting leading free holes.

## 2. Where I still disagree: types cannot be the tie-breaker on their own

Unchanged from yesterday and it is the one real objection. Of the six ties
measured this session, five have the **same type on both sides** — `x is a
number`, `send to to to to`, `sum of all things`, `send time to live`,
`x is not x`. A type filter cannot touch any of them. The one it breaks is the
postfix case you raised months ago.

Types decide **applicability**; something else has to decide **segmentation**.
That "something else" is the subject of the rest of this document.

## 3. Conceptually the rules are already one rule

R5′, R6b and R7b all refuse a name for the same reason, and it is worth stating
once because it explains why they keep needing patches:

> **A declared name may not create a second derivation of a statement.**

That is undecidable over all possible statements, so the three rules are
**computable syntactic approximations** of it. Every patch this month has been a
place an approximation was too loose (the all-glue clause) or too tight
(`to uppercase`, `time to live`). Knowing they approximate one thing tells you
how to check a fourth one, and tells you what they are converging on.

## 4. Operationally they could be deleted — measured

Your design pays for unambiguity at the **use site**: enumerate, filter, and if
more than one survives, error with brackets offered. The rules pay for it at the
**declaration site**. Two ways to buy the same thing, so the exchange rate is
measurable.

Statements generated *from* the grammar, twelve independent name sets per
configuration — because a single draw gave 3.7% and another gave 0.0%, so one
draw is not a measurement:

```
  name set   ambiguous %   (rules deleted)
         0         3.72%          4         0.00%          8         7.59%
         1         3.95%          5         0.02%          9         3.70%
         2         3.58%          6         0.00%         10         4.28%
         3         3.66%          7         3.29%         11         3.73%

  rules in force        0 / 67525   = 0.00% of statements ambiguous
  rules deleted      2091 / 66883   = 3.13% of statements ambiguous
```

**Zero out of sixty-seven thousand** with the rules in force. Not "few ties" —
none. About 3% without them, spread 0–7.6% depending on which colliding names a
program happens to declare.

So:

| | |
|---|---|
| **keep** R5′ + R6b + R7b | names are restricted; no statement ever needs a bracket it did not ask for |
| **delete** them | every name is legal — `time to live`, `is valid`, `not found`, `all things` — and roughly one statement in thirty needs a bracket |

### And minimum lookup goes with them

This is the part that is easy to miss and is the largest simplification. Minimum
lookup's job is to **pick a winner when several derivations exist** — the same
job the rules do, at the other end of the pipeline. If ambiguity is an error,
there is nothing left to pick.

So the deletion is not three rules. It is:

```
  R5'  interior glue, all-glue clause
  R6b  no name begins with a pattern's word content
  R7b  no name begins with a refining word, and its conditionality
  minimum lookup, and the tie-counting that goes with it
  the per-scope relation computation
  the "later declaration invalidates an earlier name" problem
  the declaration half of the differential check
```

replaced by one sentence: **if a statement has more than one reading, bracket
it.** Which is what you described at the start.

## 5. Which I think you should take, and the honest risks

**I lean delete**, on the language's own stated trade: a bracket costs
*writability*; a forbidden name costs *readability*. Ronin has chosen readability
over writability at every other fork, and this is the same fork.

Three risks, stated plainly:

**The number is an order of magnitude, not a forecast.** The corpus is synthetic
and there is no Ronin code to measure instead. 3% could be 1% or 8%.

**Silent captures become visible errors, which is a win, but a visible one.**
Today `send time to live` quietly means the name. Under deletion it stops
compiling until bracketed. That is better, and it is also more errors in front of
a beginner — the exact audience this language is for. The hover-shows-implicit-
brackets idea is what makes that survivable, so the two decisions are coupled.

**It invalidates work just landed.** R5′ is committed and tested. Deleting it
throws that away. Worth saying so rather than letting it be discovered.

## 6. Timing — and this one has a deadline, unlike the last

`TIME-TO-LIVE.md` §6 said narrowing a refusal is backward-compatible, so nothing
was lost by waiting. **This is different.** R7b's operator half, B₁'s multi-word
machinery, and C's type table are all being built *on the keep-the-rules branch*.
Every one of them is work that the delete branch does not need.

So unlike the last decision, this one is worth making **before** the next slice
rather than after — not because the rules are wrong, but because they are about
to acquire dependents.

Probe: `rules_or_brackets.py`.
