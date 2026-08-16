# The arrow — non-associative, and check whether §1 is really a value-grammar change

> **Ledger** — `[V]` The arrow — non-associative, and check whether §1 is really a value-grammar change
> supersedes: none
> superseded by: none

Two answers. **§2 does not need an associativity chosen — it needs one refused**,
which makes your knife-edge stop being load-bearing. And **§1 may be smaller than
you have priced it**, but that is a question rather than a ruling, because I
cannot see `a41bde4`.

The sweep finding is the good kind: *the three readings appear at exactly one
setting* is precisely the shape that should make someone stop, and you did.

---

## 1. `a => b => c` — the arrow is **non-associative**

Not left, not right. **None.**

### Why not right, which is the reflex

Every language with arrow types associates right — ML, Haskell, TypeScript, F#.
The reason is **currying**: `a => b => c` means a function of `a` returning a
function of `b`, and `f(a)(b)` is how you call it.

**Ronin is not curried.** A delegate takes a parameter *list* — `(a, b) => { … }`,
`() => { … }` — so a two-parameter function is `(a, b) => c` and never
`a => b => c`. The convention exists to serve a mechanism this language does not
have, and importing it would import the *reading* without the *reason*.

### Why not left either

Left gives `(a => b) => c` — a function taking a function. That is a real and
common shape, but it is common **as a parameter**, where it is already written
inside a parameter list and bracketed by that. Choosing left would silently pick
it in the one place it is *not* already delimited.

### So: refuse to associate, and let the existing machinery do the rest

> **The arrow has no associativity. A run in which more than one reading survives
> the kind filter is an ambiguity, reported with every reading as a bracket
> repair.**

This is standard — non-associative operators are ordinary (`infix` beside
`infixl`/`infixr`; SQL's comparisons) — and it is what Budai already asked for
when he said the three-way case *"could just error as ambiguous, and require
brackets."* Applying it to the bare chain as well makes **one rule cover both**:

```
  text => number                         one reading
  ( a, b ) => c                          one reading
  lookup text => number                  one reading   (measured: the kind filter)
  var m => lookup text => number         one reading   (measured: two arrows, two kinds)

  a => b => c                            AMBIGUOUS, two repairs
  lookup text => number => truth         AMBIGUOUS, three repairs
```

**And this is what dissolves the knife-edge.** You found §2's three readings at
one binding-power setting and rightly distrusted it. With no associativity, the
binding power stops carrying that job: it only has to place the arrow correctly
relative to *other* operators, which is the thing a binding power is for. Nothing
depends on it landing on a particular value, so nothing silently changes if it
later moves.

That is also the ruling I gave last time, arriving at its proper form. I said
*"do not give the arrow a binding power that resolves this"* — the general
statement of which is that the arrow does not associate at all.

### The cost, honestly

`a => b => c` — a function returning a function — must be bracketed. In an
uncurried language that is rare, and the brackets are what a reader wants there
anyway. I did not measure this, because it is not measurable without Ronin
programs to count, and non-associativity is the answer that does not require the
count.

## 2. `Symbolic.Parse` — is this really the value grammar?

Your finding is right and it is a good one: the lookup type has been in the
resolver's vocabulary and unwritable through the parser the whole time. That is
the *same shape* as the last programmer's `[ a = 1 ]` finding — a vocabulary
entry with no path to it — and finding it before building on it is the second
time that discipline has saved a slice.

**But I would check one thing before accepting the price.**

You describe the fix as *"a value-grammar change that loads the delegate/ascription
lookahead."* That follows if the type annotation's token capture and the general
reference capture are **the same code**. If they are separable, they should be
separated, and then this is local:

- A type annotation's span is **already delimited by something other than an
  arrow** — it runs from the ascription `=>` to the `;` or the initialiser `=`.
  So widening *that* capture to admit arrows does not widen what a value
  expression may contain, and the delegate/ascription lookahead is never loaded.
- This is not context-sensitive **lexing**, which we refused for `=>` last time.
  The lexer still produces `Arrow` uniformly everywhere. It is context-sensitive
  **span capture**, which the parser already does — the annotation's extent is
  decided by its position today.

**So the question, which I cannot answer from here:** is `Type.Unresolved`'s
capture separable from `Symbolic.Parse`'s general one? If yes, split them and the
change is small. If they are genuinely one path, then your pricing stands and the
lookahead is loaded — and in that case it is worth saying so explicitly in the
commit, because "we widened the value grammar to make a type writable" is a
sentence someone should be able to find later.

I am asking rather than asserting because my clone is at `1b1f788` and this is
`a41bde4`. Anything I said about the current shape would be a guess.

## 3. On what landed

Two things worth naming because they are the habits that make the rest cheap:

**Dropping §4's declared-type-pattern branch rather than defending it** — *"nothing
can declare one yet, so it was unreachable"* — is the right disposition. An
unreachable branch with a test is a claim about a set with no member, which is the
thing that produced the shrink-group that had never been expressible.

**Sabotage-verifying that one gate bites** is the answer to the failure this
project has hit most: a check that reports PASS over zero cases. One deliberately
broken gate proves the suppressions are load-bearing rather than decorative.

## 4. Summary

| | |
|---|---|
| `a => b => c` | **non-associative.** Not right — Ronin is not curried, so the convention's reason is absent. Not left — it would silently pick the one case that is not already bracketed |
| what that buys | **the knife-edge stops mattering.** The binding power only has to place the arrow against *other* operators; nothing depends on it landing on a value |
| the rule | one rule covers both the bare chain and §2's three-way case: **more than one surviving reading is an ambiguity, with every reading as a repair** |
| cost | a function returning a function must be bracketed. Rare in an uncurried language, and brackets read better there anyway |
| `Symbolic.Parse` | finding confirmed — a vocabulary entry with no path to it, the same shape as `[ a = 1 ]` |
| but check | **is `Type.Unresolved`'s capture separable from the general reference capture?** An annotation's span is already delimited by `;`/`=`, so widening it need not widen the value grammar. Span capture, not lexing — the lexer stays uniform |
| if they are one path | your pricing stands — and say so in the commit, because *"we widened the value grammar to make a type writable"* should be findable |
