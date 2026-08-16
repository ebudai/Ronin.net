# `is` for equality — the reference-equality worry dissolves, the bill does not

> **Ledger** — `[V]` `is` is value equality, `is a` is a type test — ruled. The checker rule it implies is a recommendation.
> supersedes: NOTHING-ANALYSIS §C
> superseded by: none

Short version: **adopt it**, your reference-equality concern turns out not to be
a concern at all, and the real obstacle is somewhere you did not look — `is` is
the **6th commonest word** in identifiers, so under R5 as written it is the
second most expensive word in the language. That is fixable, and fixing it
improves every other operator too.

Two of my own probes were wrong in this round and are corrected below rather
than quietly dropped.

---

## 1. The reference-equality worry dissolves

You wrote: *"my one issue is the rare time we need reference equality and how
good `is` would be to mean only that."*

There is nothing left for it to mean, because **for anything with identity,
identity already is its equality.**

Two boxes with the same `cash` are not the same box — they are two boxes. There
is no structural equality for an instance that competes with identity, so asking
"same instance?" and asking "equal?" are the same question and one operator
answers both:

> **`is` is equality. For anything that has identity, identity is its equality.**

| operand | `is` means | why |
|---|---|---|
| number, text, truth | value equality | no identity to have |
| list, lookup, interval | value equality | data, not entities |
| **instance handle** | **same instance** | two boxes with equal members are still two boxes |
| delegate | same delegate | nothing else is defined |

So `is` is not "value equality plus a missing operator" — it is one rule with
one sentence, and the reference case is the *default* rather than an exception.

**One decision this forces**, and it should be explicit rather than assumed: are
lists and lookups **values**? They must be, for the table above to hold. That is
the same line the struct-of-arrays decision already drew — instances are
entities with rows and handles; lists are data. If lists were references you
would need both notions for lists and the worry would come back. I would write
"lists and lookups are values; instances are entities" into the spec beside this.

If someone later wants *same contents* for two instances, it is
`a.cash is b.cash and a.lid is b.lid`, or a stdlib `(_) matches (_)`. Defer it —
it has never been the common case.

## 2. Removing `==` is right for a reason beyond taste

`=` already means something in Ronin: it separates a lookup key from its value
(`[number = 3, text = 7]`). Keeping `==` one keystroke away, with a completely
different meaning, is the C hazard imported for no benefit.

And the deeper point: `<` and `>` are *pictographic* — they look like what they
mean and collide with nothing. `==` is not; it exists **only** to avoid a
collision with `=`. Spell equality as a word and the reason `==` existed is gone.

So: words for equality and type tests, symbols for ordering. That is not
inconsistent — it is keeping the symbols that earn their place.

## 3. The bill, measured — and it is large

`glue_cost_is.py`, over 340,357 distinct multi-word identifiers:

```
  word          identifiers hit     share
  to                       5414    1.591%
  is                       5266    1.547%     <- 6th commonest word in the corpus
  a                        3417    1.004%
  not                      1720    0.505%
  an                         75    0.022%
  otherwise                   1    0.000%
```

`is` is essentially tied with `to`, which we identified as the most expensive
glue word available. And the collisions are exactly the ones that matter:

```
  is_valid   is_stream   is_short_exp   is_nonnegative_int   IsBinary
```

Under R5 as written, `(_) is (_)` reserves `is` inside every multi-word name —
so `let is valid => …` is illegal. That is the canonical boolean-name shape, and
it is the shape a spaces-in-names grammar most encourages.

**One honest deflation.** The `is_` prefix is a convention that compensates for
weak types — it tells you `valid` is a boolean. With a `truth` type, `if valid`
reads better than `if is valid`, so some of that 1.547% is a habit Ronin does
not need. Not all of it: `test_left_is_not_int` is a real name with `is` in the
middle.

## 4. R5 is stricter than it needs to be, and narrowing it is general

The `ONE-LAW` result says a word needs reserving only where some contiguous
word-only span covers a **composite** reading. An infix reading needs an operand
on *each* side. So a name can only be a rival if the glue word sits **inside**
it, with words on both sides. A name that merely *begins* or *ends* with the
word has nothing on one side and cannot compete.

`glue_position2.py`, exhaustive over three configurations:

```
  (_) is (_) + rival «check (_)»       edge glue: 12 names, 0 dangerous
                                       interior:  12 names, 4 dangerous
  (_) is (_) | (_) is not (_) | not (_) edge glue: 24 names, 0 dangerous
                                       interior:  24 names, 4 dangerous
  send (_) to (_) | send (_)           edge glue: 24 names, 0 dangerous
                                       interior:  12 names, 4 dangerous

  [PASS] with R6b doing its own work, edge glue never captures
```

> **R5′** — no multi-word name may contain a glue word **interiorly**.

**A correction on the way there.** My first run of this reported a
counterexample — the name `not x` capturing `not «x»`. That is not an infix
capture at all; `not (_)` is an anchor-only prefix pattern and this is **R6b**,
which we already adopted. The classification had to exclude R6b's own cases
before R5′ could be judged. With that fixed it passes.

**This is not an `is`-specific hack.** It makes `to` cheaper too — `to_uppercase`
becomes legal while `time to live` stays refused, which is exactly the right
split.

## 5. The article needs a third rule, and I got its fix wrong twice

Under R5′ a name may *begin* with a glue word — so `a number` becomes legal, and
then:

```
  x is a number       «x» is a «number»     3 lookups     type test
                      «x» is «a number»     3 lookups     value comparison
                      -> TIE -> ERROR
```

Loud rather than silent, and bracket-repairable — but it means declaring any
name beginning `a ` is a landmine for every type test in scope.

**Wrong fix #1: pin the article's operand.** I wrote `(_) is a <_>` through a
helper that only maps `_` to a hole, so `<` stayed a literal word segment and the
pattern was **inert**. It printed a pass because it never matched anything — the
degenerate-control failure again. Re-run with a real one-token hole, pinning
*does* remove the tie, but only because a pinned hole costs zero lookups in that
resolver. A pinned hole here is a type *reference*, not a declaration, so it
ought to cost one — and then both readings are 3 again and the tie returns.
Pinning is a bet on a cost-model decision that has not been made.

**Wrong fix #2: separate namespaces.** I claimed the type table removes the
rival. It does not: the rival is not a type lookup, it is the plain `(_) is (_)`
with the *value* name `a number` on the right. Both readings already live in the
tables they belong to. Measured, and it still ties.

**The actual rule is much smaller.** `article_rule.py` sweeps every name over the
universe and finds exactly eight that break something:

```
  begin with an article        2      «a x», «a number»
  interior glue (R5′)          4      «x is x», «number is x», …
  begin with a pattern (R6b)   2      «not x», «not number»
```

So three narrow rules cover all of it, and the third is a *prefix* rule like
R6b rather than a whole-word rule like R5:

> **R-art** — no name may begin with `a` or `an`.

## 6. What the three narrow rules cost together

```
  rule                          kills     share
  R5  blanket «is»               5266    1.547%
  R5′ interior «is»              1465    0.430%
  R5  blanket «not»              1720    0.505%
  R5′ interior «not»             1236    0.363%
  R5  blanket «a»                3417    1.004%
  R-art  begins «a»/«an»          876    0.257%
  R6b  begins «not»               443    0.130%

  TOTAL blanket (is/not/a anywhere)    10201    2.997%
  TOTAL narrow (R5′ + R6b + R-art)      3941    1.158%
  reduction                                       61%
```

And the residue is mostly corpus artefact — `a_r`, `A_f`, `a_y`, which are
single-letter mathematical names that in Ronin are one-word names, not
multi-word ones.

**The framing worth keeping:** after the narrowing, what is still refused is
what a human reader would also misparse. `y is x` reads as a comparison; `is
valid` does not. The rule now tracks the reading rather than the spelling.

## 7. `is not`, and `not`

Measured (`glue_position2.py` §3):

```
  x is not y        OK   3 lookups   «x» is not «y»
  x is ( not y )    OK   5 lookups   «x» is ⟨not «y»⟩
```

`(_) is not (_)` as its own pattern wins on minimum lookup, so `x is not y`
reads as inequality — the right answer — and "x equals the negation of y" is
still reachable by bracketing, which is the standard repair.

One cost to name: `not (_)` is anchor-only, so **R6b bans names beginning
`not`** — `not found`, `not allowed`, `not initialised`. 0.130%, and the
workaround is `missing`, `refused`, `unset`, which read better anyway. Worth
knowing rather than discovering.

`is not a` / `is not an` follow with no extra machinery.

## 8. On `a` versus `an` — accept both, enforce neither

English puts `an` before vowel *sounds*, not vowel *letters*: *an hour*, *a
unicorn*, *a user*, *an FBI agent*. A compiler that enforces the article will be
wrong in public, and being wrong about English in a language sold on readability
is worse than being silent about it.

So: `is a` and `is an` are synonyms, accepted anywhere, never corrected. The
programmer writes what reads well.

## 9. Why the article cannot simply be dropped

`x is number` and `x is a number` are **different questions**:

```
  x is number      is the value of x equal to the value «number»
  x is a number    is the type of x the type «number»
```

With separate symbol tables there is no other way to tell which side of the
language the right operand comes from — the operator has to say it. The article
is the namespace selector. That is why it reads like grammar sugar and is not.

For the *exact*-type question you also already have `type of x is number`, with
no new syntax at all. So the two shapes divide cleanly, and this settles the
`MATCH.md` §6a question that has been open:

| | question | universe |
|---|---|---|
| `x is a shape` | is x's type a case of this closed set | **closed** — exhaustiveness available |
| `type of x is circle` | is x's type exactly this | **open** — never exhaustive |

## 10. Summary

| | |
|---|---|
| `is` for value equality | **adopt** |
| reference equality | **no separate operator needed** — identity *is* equality for anything that has identity |
| lists/lookups are values, instances are entities | needs stating in the spec; the table above depends on it |
| `is a` / `is an` for type tests | **adopt**; both spellings, never enforced |
| `is not`, `is not a/an` | **adopt** — measured to read as inequality without ambiguity |
| removing `==` | **yes**, and for a reason beyond taste: `==` exists only to dodge `=` |
| ordering stays `<` `>` `<=` `>=` | yes — pictographic and collision-free |
| R5 → R5′ (interior glue only) | **recommended** — measured safe, and it is a general improvement, not an `is` concession |
| R-art (no name begins `a`/`an`) | **required** if R5′ is adopted, or type tests tie |
| the bill | 2.997% blanket → **1.158%** narrow, and the residue is what a human misreads too |

Probes: `glue_cost_is.py`, `glue_position.py`, `glue_position2.py`,
`is_article.py`, `article_rule.py`. The first three contain two wrong results
left in with their diagnoses attached — `is_article.py` §2 in particular is a
*failed* fix and is worth reading as one.
