# Postfix word patterns — admit them. This reverses `WHYNOPOSTFIX.md`

> **Ledger** — `[R]` Postfix word patterns — admit them. This reverses `WHYNOPOSTFIX.md`
> answered by: POSTFIXPATTERNS-RESULT
> supersedes: none
> superseded by: none

**Supersedes `WHYNOPOSTFIX.md` entirely.** That document concluded postfix was
banned and gave a measurement to support it. The measurement was right and the
conclusion drawn from it was wrong, because it measured the wrong property.

Self-contained; nothing else needs reading first.

---

## 1. Verdict

> **A pattern may begin with a hole.** `(_) reversed` is admissible. Prefix and
> postfix compositions produce **tie errors**, never wrong programs, and the
> language already answers tie errors with a bracket.
>
> The price: **every word of a postfix pattern becomes a reserved word**, by the
> existing R5 rule. `docs/reserved-words.txt` stops reading `RESERVED (0)`.

Not a small change, and §6 lists what must be verified before it is built.
Nothing here should be implemented on this document alone.

---

## 2. Why it was banned — the reason on record is bad, and it is mine

The first instrument, `ronin_grammar_probe.py`:

```python
# A pattern beginning with a hole is left-recursive: resolving an atom
# at position p would require resolving an atom at position p. This is
# not an implementation limit, it is a language constraint --
# user-defined infix patterns cannot coexist with unbracketed args.
```

**It was an implementation limit.** That probe is a backtracking enumerator and
a leading hole makes it recurse at the same position forever. I wrote the
workaround up as a property of the language, in a comment that explicitly denies
being a workaround, and everything downstream inherited it.

`dp_resolver.py` — the one `Resolver.cs` ports — fills cells by increasing
width, so a leading hole reads only strictly smaller spans and terminates:

```
a reversed              OK   2 lookups   «a» reversed
a b reversed            OK   2 lookups   «a b» reversed
a reversed reversed     OK   3 lookups   «a» reversed reversed
```

Two things then gave the ban post-hoc support without anyone testing it. R6
rejects leading holes *incidentally* — an empty anchor run is a prefix of every
non-empty one. And R7's "infix belongs to the symbol layer" made it sound
principled. Neither is a measurement.

## 3. Do types rescue it? Partly, and not where it matters

Budai's objection to my first example was correct. In

```
sum of a reversed
```

`sum of a` is a number and `reversed` wants a list, so the reading
`(sum of «a») reversed` is ill-typed and never reaches the cost comparison.
Type filtering does real work here.

But the rescue needs the types to *differ*. Make both operations `list → list`:

```
patterns «sorted (_)» and «(_) reversed», name «xs»

    sorted xs reversed   ->   TIE, cost 3, 2 derivations

      sorted («xs» reversed)     list -> list -> list    typechecks
      (sorted «xs») reversed     list -> list -> list    typechecks
```

Both well-typed, both cost 3, and **semantically different** — sorting a
reversed list is not reversing a sorted one once the sort is stable and the keys
are not distinct.

`T → T` is precisely where postfix modifiers live: `reversed`, `sorted`,
`rounded`, `trimmed`, `normalised`. So types kill the compositions nobody would
write and survive on the ones everybody would.

## 4. The property that decides it: tie, not capture

This is what `WHYNOPOSTFIX.md` measured without noticing what it had measured.

**Every prefix/postfix clash is an *exact* tie.**

```
sorted xs reversed    TIE   cost 3
sum of a reversed     TIE   cost 3
a x b                 TIE   cost 3
a x to y b            TIE   cost 4
```

That is not luck, it is arithmetic: `f (a g)` and `(f a) g` look up `f`, `a` and
`g` exactly once each, so the two readings always cost the same. `run_dp2.py`
found the same invariance for symbols — *"lookup count is INVARIANT under
regrouping"* — and it is why symbols need binding powers at all.

**So the failure mode is a compile error, not a wrong program.** The hazard R5
exists to prevent is *silent capture*: a wrong reading that wins outright.
Cost invariance makes silent capture impossible for this shape. A tie stops the
compiler and asks for a bracket, which is what the language does with every
other ambiguity and has since the first rule anyone wrote down.

I reported 120 ties as though they were 120 defects. They are 120 requests for a
bracket.

## 5. The one silent hazard, and R5 already covers it

There *is* a capture available, and it is the ordinary R5 one:

```
names {x}          «x b» ->  2 lookups   «x» b        the pattern applies
names {x, x b}     «x b» ->  1 lookup    «x b»        the name swallows it
```

Strictly cheaper, silent, a different program. But in `(_) b` the word `b`
appears *after the first hole*, so it is **glue**, and R5 reserves it — a
multi-word name may not contain it, and `x b` cannot be declared.

Every word of a leading-hole pattern is after the first hole, so **a postfix
pattern reserves all of its words.** `glue.py` already computes this and already
labels it: *"POSTFIX PATTERNS (leading hole — EVERY word is glue, most expensive
shape)"*. What I did not notice is that the expense is also the safety.

## 6. So the criterion is bracketing burden, not safety

All three leading-hole kinds are safe. They differ in how often the author has
to bracket:

| leading hole | measured | brackets required |
|---|---|---|
| `{_}` bracket-delimited | 10,490,112 resolutions, **0 ties** | never — they are already there |
| `(_)` free | 322,560 resolutions, **120 ties (0.04%)** | when composing prefix with postfix |
| `<_>` one token | **1,134 ties** | constantly — unusable, though still not unsafe |

And the practical point that decides it: **a single postfix use never ties.**
`xs reversed`, `total rounded`, `name trimmed` — no prefix pattern in the
statement, no clash. The tie appears only on composition, which is exactly where
a reader wants the brackets too. `sorted xs reversed` is ambiguous to a human
reader as well; requiring `(sorted xs) reversed` is the language declining to
guess where a reader could not either.

## 7. What this costs

**Zero reserved words ends.** `reversed`, `sorted`, `rounded` and every other
postfix word joins the registry. That property was hard-won and should not be
given up by accident — but the words being spent are cheap ones. Losing
`reversed` inside multi-word names is not losing `in`, `of` or `to`.

The registry should show them, and the `RESERVED (n)` count becoming non-zero
should be a reviewed diff rather than a surprise.

## 8. What must be verified before this is built

Three things, none of which is optional:

**a. R6 needs re-deriving.** It currently rejects a leading hole incidentally,
because an empty anchor run is a prefix of every non-empty one. If leading holes
are admitted that comparison has to be replaced, and it is not obvious what
with: two postfix patterns `(_) b` and `(_) b (_)` are prefix-related in their
post-hole segments and would clash. **I do not have a rule to propose here** —
it needs the same exhaustive treatment `LEADINGHOLES.md` gave the bracketed
case.

**b. The 0.04% is an over-estimate and a narrow one.** That run did *not* apply
R5 to the leading-hole patterns, so some of those 120 ties disappear once the
postfix words are reserved. It also used 2–3 word statements and every fourth
name pair, because the full corpus ran past ten minutes. It is a signal, not a
number to quote.

**c. The tie rate over *real* programs is unmeasured.** The corpus is random
word sequences, most of which resolve to nothing. Every composition of a prefix
and a postfix operation ties, and those are natural things to write — so the
rate in meaningful code is higher than 0.04%, possibly much higher. That is the
number that decides whether the bracketing burden is acceptable, and nobody has
it.

## 9. Supersession entries

| document | status |
|---|---|
| `WHYNOPOSTFIX.md` | **superseded entirely** by this document. Its §2 measurement stands; its §3 conclusion does not |
| `INTERVALSANDINDEXING.md` §"ladder" | **amended** — `4..20 reversed` moves from "needs per-pattern binding powers" to "needs R6 re-derived". Binding powers were never the blocker; cost invariance means there is nothing for them to decide |
| `ronin_grammar_probe.py` left-recursion comment | **wrong**, and should be corrected in place rather than left to mislead the next reader |
| `RONINGRAMMAR.md` R7 | unaffected — symbols remain the place for *symbolic* infix. This is about word patterns |
