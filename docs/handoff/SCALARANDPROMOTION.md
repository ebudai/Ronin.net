# Scalar, two rungs, silent promotion — accepted, with one measurement that changes the shape and one pushback

> **Ledger** — `[V]` verdict. Scalar only, two rungs, silent promotion — accepted; every `±∞`/`NaN` path is an `Error`; the SoA correction (§5 — promotion evicts a value from SoA); a reactive cell's unbounded growth needs a watchdog, not a limit. **§7–§8 (the `fast number` denormal and the underflow sibling) are raised but were never ruled — they do not bind.** §2's exactness-as-type proposal is struck by `EXACTNESSISAVALUE`.
> answers: NUMBERRUNTIME
> supersedes: none
> superseded by: EXACTNESSISAVALUE §2

**Accepted:** scalar only, no complex, no exact irrationals, no `large number`,
silent promotion, ellipsis for display, overflow in `fast number` is a runtime
error.

**One measurement changes what silent promotion needs** (§3–§4): promotion is
bounded for the programs you'd expect and **unbounded for a reactive cell**, at
roughly one denominator digit per update, forever. That is your flagship
construct, so promotion needs a watchdog — not a limit.

**One pushback** (§8): the denormal should not be an error.

**And one correction** (§5): SoA does not absorb promotion. Promotion is the
thing that evicts a value from SoA.

---

## §1 — what "no complex, no irrationals" buys, and it is bigger than it sounds

Two clarifications, then a consequence you will like.

**There are irrational values; there is no exact irrational representation.**
`square root of (2)` still has to answer. Your existing boundary already handles
it — past roots and transcendentals the result is a float. So "no irrationals"
is not a new refusal, it is the boundary restated, and nothing changes.

**No complex means the domain errors become the mechanism.** `square root of
(-1)` has no scalar answer. In IEEE it is `NaN`; with complex it is `i`; with
neither it is an **`Error`** — same as dividing by zero, same as indexing past
the end. Likewise `log 0` and `log (-1)`.

Put that beside your overflow decision and something clean falls out:

> **Neither rung has `±∞` or `NaN`. Every path that would produce one is an
> `Error`.**

That retires the inconsistency in the tree — `/` refusing an infinity that `*`
silently produces — not by patching `*` but by making the rule general. And it is
cheap: one `double.IsFinite(result)` after each `fast` operation catches
overflow, underflow-to-infinity and NaN together, in a single predicted branch.
Also delete *"infinite or undefined"* from the guide; it now describes nothing.

## §2 — make the exactness boundary a **type** boundary

With only two rungs, this falls out almost for free, and it is worth taking
deliberately.

If `square root of (_)` returns **`fast number`**, then inexactness is carried by
the type rather than by the value — which means the compiler already tracks it,
the IDE can already show it, and you get a guarantee that is checkable by
reading:

> **Exactness is visible in the type. A `number` is exact; a `fast number` is
> not.**

The alternative is that a float enters the exact tower and silently contaminates
everything downstream — `square root of (2) * square root of (2)` quietly not
being `2` — which is the same silent-poison shape `Divide` already refuses,
minus the visibility.

**One tension worth naming:** the word is `fast`, and a square root's result is
not *fast*, it is *approximate*. `fast` then means "the IEEE double
representation," with both speed and approximation following from it. I think
that is acceptable — one word for one representation beats two words for one
thing — but it will read oddly the first time someone sees `fast number` come
back from `square root of`. The smaller open question is whether crossing the
boundary needs an explicit annotation or may be inferred; my lean is **inferred**,
with the IDE rendering inexact-typed values distinctly, since you have already
ruled the editor answers this class of question.

## §3 — silent promotion, measured: bounded where you'd hope, unbounded where you live

Accepted — and the batteries-included argument is the right one; the user did not
ask to think about widths. But the failure mode of exact rationals is not
magnitude, it is **denominator growth**, so I measured which ordinary programs
suffer it. Denominators, in digits (64 bits holds 19):

```
                                        n=10   n=50   n=100   n=500   n=1000   promotes at
  sum of 2-decimal money amounts           2      1       1       1        1    never
  running average of readings              1      2       2       3        1    never
  running total split three ways           2      1       1       1        1    never
  harmonic sum  1/1 + 1/2 + ... + 1/n      4     22      40     216      433    n = 47
  feedback      x = x/3 + 1                5     24      48     239      478    n = 40
```

**The narrow condition is clear.** Denominators stay small when many terms
*share* a small set of divisors — money, averages, splits, anything measured. They
grow when either the set of divisors keeps growing (the harmonic sum multiplies
in a new prime each time) or **a division's own result is divided again**. The
second is the dangerous one, because it compounds: the denominator is `dⁿ`, so
the digit count grows *linearly in the iteration count and never stops*.

### And the second case is your flagship construct

Exponential smoothing — `smoothed = smoothed * 0.9 + reading * 0.1` — is feedback.
It is also the single most ordinary thing a reactive cell does. Measured:

```
  after     10 updates     13 digits
  after     40 updates     43 digits    <- past 64 bits
  after    120 updates    123 digits
  after   3600 updates   3603 digits    <- one minute at 60 updates/second
```

**One digit per update, forever.** After an hour that cell's denominator is a
quarter of a million digits, and every operation on it runs a GCD over that.
Memory grows without bound, and the program does not crash — it grinds.

Note what this does to an argument I made in the last document. I said the
always-running IDE *mitigates* silent promotion, because you would see the
slowdown. For a batch computation that is true. **For a reactive cell it is
exactly backwards**: a program that runs forever is what turns unbounded growth
from a visible slowdown into a fatal one. I had it the wrong way round.

## §4 — so promotion needs a watchdog, not a limit

Do not reintroduce `large number` and do not cap the width — a cap is silent
precision loss, which is the one thing already refused. Instead:

> **The runtime notices when a value's denominator passes a width and reports it
> as a finding naming the cell.** Not an error — a diagnostic: *"this cell's exact
> value now needs 400 digits and is growing by one per update. It is probably
> meant to be `fast`."*

That is the language's existing shape applied here: the editor surfaces it, the
source records the fix, nothing silently degrades and nothing silently stops. It
also costs nothing on the hot path — the check is a bit-length test on the slow
path that already allocated.

The threshold is a tuning question, not a design one. What matters is that the
condition is *growth*, not size: a stable 200-digit denominator is fine; a
denominator gaining a digit per second is the bug.

## §5 — the SoA correction

> *"with the built-in soa layout its not a big slowdown I think"*

I think this is backwards, and it is worth catching now because it lands on
storage work already deferred.

A column is fast because it is **unboxed and fixed-width** — `long`s end to end,
scanned linearly, vectorisable. A promoted rational is a **variable-width heap
object**, so a column of them is a column of *pointers*, and every read is a
chase. **Promotion is not something SoA absorbs; promotion is what evicts a value
from SoA.** The moment one element promotes, the naive layout loses the property
the whole design exists for.

That is a solvable problem and the design answer is probably per-column rather
than per-element: the column carries a representation tag, stays `long`/`long`
while every element fits, and promotes **as a whole** when any element needs it —
so the cost is one visible event rather than a pointer chase per row, and the
watchdog in §4 has a natural place to live. A side-table for the few promoted
rows is the other shape. Either way this belongs in the storage design, and it
should be settled with it rather than assumed away.

## §6 — printing: agree, and here is the reason the dot fails

Your instinct is right, and the reason is sharper than "more trouble than it's
worth". Every rational has a terminating or repeating decimal, so `0.3̇` is not a
special case — it is the *general* rule, which sounds appealing until you measure
the block:

```
  1/3       repeating block:        1 digit
  1/7                               6 digits
  1/17                             16 digits
  1/97                             96 digits
  1/999983                    999,982 digits
```

The notation is total and **unbounded**. It cannot be the default. (It is also a
combining character that renders unreliably outside a good font.)

Two things worth building instead:

**Print the ellipsis only when the rendering is lossy.** `0.5` prints `0.5`;
`1/3` prints `0.333…`. Then the ellipsis is not decoration — its presence means
*this display is not the value*, which is true, useful, and exactly the kind of
signal the rest of the language trades in.

**And make the exact form reachable.** The value *is* `1/3`; a decimal is a lossy
rendering of something the language went to trouble to keep exact. Hover shows
the fraction. There is even a case for the fraction being the default print —
it is shorter, exact, and universally read — but decimal-with-ellipsis is the
safer default for a RAD audience, so I would take that and put the fraction one
hover away.

## §7 — `fast number` overflow: agreed, and the reason is the word

> *"multiplying two large doubles should promote, but that only happens with fast
> number, right? if so, runtime error."*

Right on both halves. Under `number` the multiply promotes and no infinity is
reachable; under `fast number` the hardware produces `+∞` and that should be an
`Error`. The reason worth writing down is that **the word is `fast`, not
`IEEE`.** Someone writes `fast` to ask for speed. They have not thereby asked for
infinity semantics, gradual underflow, or NaN propagation, and it would be a
strange bargain if a request for speed silently bought a different arithmetic.

**Two consequences to accept openly rather than discover:**

**There is now no IEEE escape hatch.** Some real algorithms want `±∞` as a
sentinel — initialise a minimum-search at `+∞`, let it propagate. Under this rule
Ronin cannot express that, and the workaround is `otherwise` or an explicit
guard. I think that is the right trade for this audience, but it should be a
*stated refusal*, not a gap someone finds.

**Underflow is the sibling you did not mention, and it is worse.** If
`1e300 * 1e300` is an error, what is `1e-300 * 1e-300`? It is `0`. Overflow at
least announces itself as an infinity; underflow produces a **plausible-looking
answer** and every downstream reader believes it. By the same principle it should
be an `Error` too — and `double.IsFinite` does not catch it, so it needs its own
test: a non-zero result that underflows to zero.

## §8 — the denormal: I would not make this an error

The strongest version of your case first: a denormal usually means a computation
has already lost almost all its precision, so refusing one catches the bug
earlier than the eventual zero or infinity does. That is a real argument.

But three things:

**It is not the hazard, it is a proxy for it.** `x / 1e-320` would error while
`x / 1e-300` would not — even though both can overflow, and the second is far
likelier to appear in real code. The actual hazard is *the result*, and you have
already covered the result in §7. Adding the operand rule catches a different,
narrower set and misses most of what it is aimed at.

**A denormal is a valid, exact number.** `5e-324` is a value, not a defect.
Refusing to divide by it refuses a legal computation because of how the divisor
happens to be represented — which is the same category error as making a date
literal's meaning depend on its own field values.

**It only exists where you already asked for speed.** The exact tower has no
denormals at all — small rationals are just small rationals. So this rule applies
only inside `fast number`, where the user asked for the hardware.

If the real concern is **performance**, that is a different and legitimate rule:
denormal arithmetic is slower on real hardware, and flush-to-zero is the standard
answer — but it is *silent precision loss*, so it collides with §7's underflow
decision and the two must be decided together. If the real concern is **"a
denormal means something already went wrong,"** that is a diagnostic, and it
belongs where §4's watchdog belongs: the IDE says so, the source records the fix.

Divide-by-zero as an error: already implemented, already right, no change.

## Summary

| | |
|---|---|
| scalar, no complex, no exact irrationals | **accepted.** There are irrational *values*; the roots-and-transcendentals boundary already handles them, so nothing changes |
| the consequence | **neither rung has `±∞` or `NaN`** — every path that would produce one is an `Error`. `square root of (-1)` is an error, not a NaN. One `IsFinite` test per `fast` operation catches all of it |
| also | delete *"infinite or undefined"* from the guide — it now describes nothing |
| exactness boundary | make it a **type** boundary: `square root of (_)` returns `fast number`, so inexactness is visible in the type rather than contaminating silently. Tension: `fast` will name a thing that is approximate rather than quick |
| no `large number`, silent promotion | **accepted** — and bounded in practice for money, averages, splits and measurements (**measured**: never promotes in 1,000 steps) |
| but | growth is unbounded when **a division's result is divided again**. `x = x/3 + 1` promotes at n=40 |
| **the finding** | exponential smoothing is feedback, and it is what a reactive cell does. **One denominator digit per update, forever** — 3,603 digits after a minute at 60 Hz. Memory grows without bound and the program grinds |
| I had this backwards | I said always-running *mitigates* promotion. For a batch computation yes; **for a reactive cell it is what makes it fatal** |
| so | a **watchdog, not a limit** — the runtime reports a finding naming the cell when a denominator's width keeps growing. Never a silent cap; that is precision loss |
| **SoA correction** | promotion is not absorbed by SoA — **promotion is what evicts a value from SoA**, since a promoted rational is a variable-width heap object. Likely answer: promote **per column**, not per element. Settle it with the storage design |
| printing | agreed on the dot, and the reason is that the repeating block is **unbounded** — 96 digits for `1/97`, ~10⁶ for `1/999983` |
| instead | ellipsis **only when the rendering is lossy**, so its presence means *this is not the value*; exact fraction one hover away |
| `fast` overflow → error | **agreed**, because the word is `fast`, not `IEEE` — a request for speed did not buy a different arithmetic |
| consequence to state | there is then **no IEEE escape hatch** (no `±∞` sentinel). Right trade, but write it down as a refusal |
| the sibling you did not name | **underflow.** `1e-300 * 1e-300` is `0` — a plausible-looking answer, which is worse than an infinity. Same principle says error; needs its own test |
| **denormal → error** | **I would not.** It is a proxy for the hazard you already covered, it refuses a valid exact value over its representation, and it lives only where the user asked for hardware. If the concern is speed that is flush-to-zero (a different rule, colliding with underflow); if it is "something already went wrong" that is a diagnostic |
