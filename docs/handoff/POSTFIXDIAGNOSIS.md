# The missing derivation — located, and a narrower rule that solves §3(d) and §8(a)

> **Ledger** — `[R]` The missing derivation — located, and a narrower rule that solves §3(d) and §8(a)
> answers: POSTFIXPATTERNS-RESULT
> supersedes: not yet checked
> superseded by: not yet checked

Answering `POSTFIXPATTERNSRESULT.md`. Three things: where the second reading
lives in the reference, a narrowing that makes the index problem and the R6
problem both go away, and a concession on §8(c).

---

## 1. The reference offers both, and they live in different atom classes

`POSTFIXPATTERNS.md` §8(e) was right to ask for this on `Resolver.cs`, and §8
should have said so. Meanwhile, here is exactly where the two readings sit in
the reference table, so the port has something to be checked against.

```
tokens: sorted  xs  reversed          pattern_bp = 7

CLOSED atoms — patterns ending in a WORD, names, brackets
  Ac[1][2]  xs                    cost 1   «xs»
  Ac[1][3]  xs reversed           cost 2   «xs» reversed
  Ac[0][3]  sorted xs reversed    cost 3   ← (sorted xs) reversed

OPEN atoms — patterns whose LAST segment is a hole
  Ao[0][2]  sorted xs             cost 2   sorted «xs»
  Ao[0][3]  sorted xs reversed    cost 3   ← sorted (xs reversed)

the full span
  E[0][3][m=0]   cost 3   derivations 2      ← the tie
  E[0][3][m=7]   cost 3   derivations 2
  E[0][3][m=8]   cost 3   derivations 1      ← only the closed one survives
```

**The two readings are in different classes.** The postfix reading is a *closed*
atom; the prefix reading is *open*, because `sorted (_)` ends in a hole. They
meet only where `m <= pattern_bp` — `expr()` merges `Ao` under exactly that
condition.

And the reading the port returned is the **closed** one, which is what you get
if the open atom is not merged at that span.

### So the check is two questions, in this order

**Does the port build `Ac[1][3]` — `«xs» reversed` — when [1,3) is reached as
an argument rather than as the whole statement?** That inner span begins with
`xs`, a name, which keys nothing in the anchored index. If the early return at
`:108-116` fires before leading-hole patterns are consulted *for that span*,
`Ac[1][3]` never exists, and then `Ao[0][3]` cannot be built either, because
`sorted`'s trailing argument reads `E[1][3][pattern_bp]` and finds nothing.
That would produce exactly one derivation, and exactly the one you saw. It is
consistent with `xs reversed` resolving fine on its own, because at top level
the span is the whole statement.

**If `Ac[1][3]` does exist, is `Ao[0][3]` merged into the top-level cell?**
i.e. is the statement resolved at a minimum binding power `<= pattern_bp`. At
`m = 8` the reference also returns one derivation, and the same one.

Either answer localises it. What I can say is that **the algorithm reaches both**
— so on current evidence this is a port divergence, not a property of the DP, and
§4's safety argument stands conditional on the port being made to match. If it
turns out `Resolver.cs` must differ for a reason, then postfix has silent capture
and the reversal fails; that is the fork, and it is now a small measurement
rather than an open question.

## 2. §3(d) — and a narrowing that fixes it and §8(a) together

Your instinct to index by the last word is right, and it generalises into a
rule that solves more than the index.

| shape | first word | last word | indexable |
|---|---|---|---|
| `sorted (_)` — prefix | yes | no | by first |
| `(_) reversed` — **postfix** | no | **yes** | **by last** |
| `(_) at (_)` — infix | no | no | **neither** |

A postfix pattern always ends at the span end, so it can be keyed on `t[j-1]`
exactly as an anchored one is keyed on `t[i]`. One extra dictionary, symmetric
code, and the `ResolverCost.cs` ceiling keeps its optimisation. **Only word
*infix* is unindexable** — and word infix is the one thing nobody has asked for,
because R7 already puts infix in the symbol layer where it belongs.

So narrow the proposal:

> **A pattern may begin with a hole only if it ends with a word.** Postfix is
> admitted; word infix stays banned.

That gives you `reversed`, `sorted`, `rounded`, `trimmed` — every form in this
conversation — and it costs no index.

### And it lets me propose the R6 rule I said I could not

I said R6 needed re-deriving and I had nothing to offer. Under the narrowing I
do, because the shape becomes symmetric:

> **Anchored patterns' leading word-runs must be prefix-free. Postfix patterns'
> trailing word-runs must be suffix-free.**

`(_) sorted` and `(_) sorted twice` clash for the mirror image of the reason
`send (_)` and `send urgently (_)` do. One rule, two directions, and the
existing implementation is half of it already.

**This still needs the exhaustive treatment** `LEADINGHOLES.md` gave the
bracketed case — I am proposing a rule, not reporting a verified one, and the
cross-class interaction in §1 is precisely the kind of thing a rule derived by
symmetry can miss.

## 3. §8(c) — you are right, and I listed an unactionable item as actionable

There is no corpus and the language cannot yet express what one would contain.
Asking for a tie rate over real programs was asking for something nobody can
produce, and dressing it as an open item made it look like work rather than a
blocker.

The honest substitute is to decide on the **worst case** instead of the rate:

> Assume every composition of a prefix and a postfix operation ties. Is
> requiring `(sorted xs) reversed` acceptable?

That is answerable today, by judgement, and it is Budai's to make. It also has a
defensible answer — `sorted xs reversed` is ambiguous to a *reader*, so the
bracket is information rather than ceremony — but it is a taste call and should
be recorded as one rather than deferred to a number that will never exist.

## 4. Agreed and unqualified

- **The `Resolver.cs:725-727` comment is worse than the probe's**, because it is
  load-bearing prose in the shipped compiler asserting a language constraint
  that is not one. Correct it in the same pass, whichever way the decision goes.
  It was mine, and it has now propagated into two codebases.
- **Postfix∘postfix is unambiguous** — `xs reversed sorted` reads left to right
  and never ties. That extends §6 and is a point in favour: the composition that
  *does* tie is prefix∘postfix, which is the one a reader also finds ambiguous.
- **Nothing implemented, and correctly so.** §5's order is right, with §2 above
  slotting in as a cheaper first step than "settle §8(a)": check the two cells,
  then decide whether the narrowing removes the R6 question entirely.
