# "Don't do that" — where it holds, and how much of the last rule it takes back

> **Ledger** — `[V]` "Don't do that" — where it holds, and how much of the last rule it takes back
> supersedes: not yet checked
> superseded by: not yet checked

You are right, and it is worth saying how consistently. On this one axis my
prior has been miscalibrated in the same direction every time:

| | I said | you pushed | outcome |
|---|---|---|---|
| `time to live` | refuse | should be legal | you were right — the hazard needs a sibling pattern |
| `is valid`, `to uppercase` | refused by R5 | too broad | you were right — R5′, 61% of the bill gone |
| `not x` | blanket refuse | "who names something starting with not" | you were right, and it cost nothing |
| `wait` / `wait until` | blocked by R6 | "shouldn't produce any ambiguity" | you were right — 7 in 5456, all repairable |
| the four name rules | keep | delete | you were mostly right — three of four went |

Five for five in one direction. The lesson is not "be less careful" — the
measurements caught real things — it is that **the blanket form of a rule is
almost always over-refusing, and the exact condition is almost always narrow
enough to state.** I should reach for the measurement before the rule, not after.

So: where does "don't do that" stop, and how much of the one rule I held does it
take back?

---

## 1. The line, stated precisely

Every language has legal-but-don't constructions, and they work because of a
property nobody usually names:

> **"Don't do that" is available when doing it costs only the person who did
> it, and they can see it. It is not available when it removes something from
> the language.**

`a[i++] = i++`, shadowing a builtin, `==` in JavaScript — all local, all visible,
all the author's own problem. That is the whole category and it is large.

The self-ambiguity rule is the one place that test fails. `wait time` as a name
does not make your code confusing; it makes `wait time` — the *call* — unwritable
by anyone, because brackets **group** and do not **classify**. `wait (time)` is
still the call. There is no spelling for the name reading. That is an
expressiveness hole rather than a style hazard, which is why I held it while
letting the other three go.

## 2. But brackets are not the only disambiguator

Types are. And filtering by well-typedness is not a silent pick — it is
**elimination**, which is exactly the tie-break in your original design.

So the question is how much of the rule a type checker takes back.
`dont_do_that.py`:

```
  send price       statement    1 survivor   UNIQUE  <- send «price»
  send price       value        1 survivor   UNIQUE  <- «send price»
                   a name colliding with an ACTION pattern

  sum of items     statement    0            no reading
  sum of items     value        2            ambiguous
                   a name colliding with a VALUE pattern, same type
```

**Action patterns are recovered entirely.** The call is `nothing`; the name is a
value; the position decides. `send price` as a statement is the call, as a value
it is the name — both legal, no rule needed, and nothing silent about it because
the filter is a hard constraint rather than a preference.

**Value patterns are not.** `sum of items` reads as a number either way, so
nothing eliminates either.

And the model above is *coarser* than what Ronin will actually have. Outward-in
typing (`GENERICS-II.md` §4) gives the **expected** type at most positions, not
just "value or statement" — so `var n => number = x is y` eliminates the
comparison, which is a truth, and the name survives uniquely. The real residue is
narrower than the table suggests: **only collisions where both readings have the
same type in the same position.**

## 3. Which lands where you want it

The names the current rule costs are `wait time`, `send queue`, `print job`,
`sort order` — all colliding with **action** patterns. The type filter takes
**all of them back.**

What stays refused is a name that duplicates a **value** pattern's own
computation: `sum of items` beside `sum of (_)`. Which is a name you would rarely
write, because the pattern already computes it — and where "don't do that" cannot
help, because there is nothing for the compiler *or the reader* to go on. Two
readings, same type, same place, no signal.

So the honest statement is neither "the rule is wrong" nor "the rule is needed":

> **The self-ambiguity rule is a pre-type-checker approximation, and it should
> shrink to *a name may not have another reading of the same type in the same
> position* when the type checker lands.**

## 4. What to do about it now

The type checker does not exist — it is what the generics conversation was for.
So the rule ships as specified, and the shrink is **scheduled rather than
hoped for**:

1. **Write the shrink into the rule's comment**, in the form the programmer used
   for the `InjectedBy` exemption — *this is the pre-type-checker form of X*, not
   a justification. A comment that states its own expiry is the difference
   between a tracked approximation and an invariant nobody dares touch. Fourth
   time that distinction has mattered this month.

2. **Tag the tests by which ones expire.** The cases that survive the shrink
   (`sum of items`) and the cases that do not (`wait time`, `print job`) should be
   separated in the suite now, so landing the type checker is a *deletion* of a
   tagged group rather than a rewrite of a file. Cheap now, expensive later.

3. **Do not relax it early.** Between now and the type checker, relaxing it makes
   programs unwritable with no diagnostic — the one failure mode worse than a
   refused name.

## 5. And the general form, since this keeps recurring

Four rules have now followed the same arc: blanket → measured → narrow →
scheduled-to-narrow-further. That is not four accidents. It suggests a default
worth adopting explicitly:

> **State the exact condition. If it cannot be computed yet, ship the blanket
> approximation with its expiry written down.**

Every rule that has bitten us was a blanket form whose reason had been forgotten,
and every one we have unwound was unwound by measuring the exact condition. The
approximations are fine — what causes the damage is losing track that they *are*
approximations.

Probe: `dont_do_that.py`.
