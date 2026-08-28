# You understood me correctly, and you are right — exactness belongs on the value, not the type

> **Ledger** — `[V]` verdict. Exactness belongs on the value, not the type — a value tag, not a second type; `fast` is a representation choice, so the checker sees one `number`. Overturns `SCALARANDPROMOTION` §2's type-boundary proposal (a rule that roots return `fast number` throws away exactness that was sitting right there).
> supersedes: SCALARANDPROMOTION §2
> superseded by: none

**§2 of the last document was wrong.** You have a better objection than the one
you made, and it kills the proposal outright: **`square root of (4)` is exactly
2.** A rule that says roots return `fast number` throws away exactness that was
sitting right there, and makes `square root of (9) is 3` compare a double against
an exact 3.

The fix keeps everything else and changes where the flag lives.

---

## §1 — what I got wrong

I moved inexactness into the type because I wanted it *visible*. That instinct
was right; the mechanism was not. Exactness is not a property of the operation —
it is a property of the **result**:

```
  square root of (4)     -> 2                exact
  square root of (2)     -> 1.41421…         no exact scalar answer
```

Same operation, same static type, different exactness. A type-level flag has to
answer for both, so it takes the pessimistic branch and loses the exact case
permanently. And your objection stands on its own besides: **`fast` is a word the
user writes.** Having an operation hand it back puts a request in their mouth
they never made.

## §2 — the fix: exactness is a tag on the value

`number` stays one type. A value inside it is *exact* or *inexact*, and roots and
transcendentals produce the exact answer whenever one exists and an inexact one
otherwise. Inexactness is contagious in the ordinary way — inexact combined with
anything is inexact.

**This needs no new machinery.** The representation already has to discriminate
integer / rational / promoted, so *inexact* is one more case of a tag that is
already being read. The exactness test for a root is cheap at 64 bits: a rational
has an exact square root exactly when its normalised numerator and denominator
are both perfect squares.

And the guarantee becomes truer, if weaker:

> **The language is exact wherever exactness exists. Where it does not, the value
> says so.**

That is the promise you can actually keep. "No `fast` in your source means
everything is exact" was never keepable — `square root of (2)` has no exact
scalar value and no amount of type discipline creates one.

## §3 — and this makes `fast` mean one thing

The best consequence. Under the value-tag design:

| | what it is | what it costs |
|---|---|---|
| exact `number` | integer or rational, tagged | the tower |
| inexact `number` | an approximation, tagged, still inside the error discipline — no `±∞`, no `NaN`, overflow is an `Error` | the tower's dispatch |
| `fast number` | the IEEE double representation, chosen by the user | nothing — an unboxed `double[]` column, vectorisable |

So `fast` is a **performance** choice and nothing else, which is what the word
says. Inexact-`number` and `fast number` are not the same thing: the first is the
language doing its best and telling you, the second is the user trading the
error discipline and the tag dispatch for raw speed and a flat column.

Before, `fast` would have meant two unrelated things — *I chose speed* and *this
happens to be approximate* — and the second would have arrived without anyone
choosing it. That was the real defect in §2, underneath the lost exactness.

## §4 — visibility, which the display rule already handles

The reason I reached for the type was that a float silently entering an exact
tower is the silent-poison shape. Under the tag it is not silent, and the marker
already exists: **the ellipsis prints when the rendering is lossy**, and an
inexact value's rendering is always lossy. So `1.41421…` marks itself, exactly as
`0.333…` does.

The two cases are not identical and the IDE is where that lands:

- `0.333…` — the **value** is exact, only the display is approximate. Hover shows
  `1/3`.
- `1.41421…` — the **value** is approximate. Hover says so, and there is no exact
  form to show.

Same marker in the text, different answer on hover. That is the standing pattern:
the editor answers the display question.

## §5 — the residual, and the compiler can see it

The real hazard that survives is equality across exactness:

```
  square root of (2) * square root of (2) is 2      ->  false
```

That is honestly false — the left side is an approximation and `is` is value
equality all the way down — but it will surprise someone. The useful part is that
**the compiler knows statically** when an equality has an operand that can be
inexact, so this is a diagnostic rather than a mystery: *"comparing an inexact
value for equality; `is close to` may be what you want."* Same shape as the
denominator watchdog — the tooling names it, the source records the fix.

Worth noting for later, not now: roots and transcendentals could be evaluated at
higher precision and rounded, so an inexact result is correct to its last
displayed digit. That is the "should not need to know computer math" goal taken
one step further, and it is a bounded cost. It changes nothing structural, so it
can arrive whenever.

## §6 — per-column promotion: agreed, and two things it needs

Good — and two consequences worth building in from the start.

**A column that promotes must be able to demote.** Otherwise it is a ratchet: one
transient spike permanently costs every element, forever, in a program that
never restarts. The cheap fix is exact rather than heuristic — keep a **count of
elements currently needing more than 64 bits**; increment and decrement on write;
demote when it reaches zero. That is O(1) per write and needs no scan.

**Leave room for three states, not two.** If one element in ten thousand needs 400
digits, promoting the whole column boxes 9,999 values that were fine. The
alternative is a column that stays unboxed with a small side-table of promoted
indices — one predicted branch per read instead of a pointer chase per read.
Which wins depends on whether promotion is rare-and-clustered or widespread, and
that is measurable later; what matters now is that the column's representation
tag has room for *unboxed*, *unboxed with an overflow table*, and *promoted*, so
the measurement can decide without a rewrite. Same shape as the requirement-value
ruling: shape the case so growing it is not a rewrite of every construction site.

**And one thing per-column promotion does not solve.** The fatal case from the
last document — a smoothing cell gaining a denominator digit per update — is a
**scalar**, not a column. Per-column promotion fixes the *storage* problem;
the *growth* problem still needs the watchdog. They are two problems and only one
is addressed here.

## Summary

| | |
|---|---|
| your objection | **upheld, and it is stronger than you put it** — `square root of (4)` is exactly 2, so a type-level rule loses exactness that existed |
| the other half | `fast` is a word the **user** writes; an operation must not hand it back |
| the fix | **exactness is a tag on the value, not a property of the type.** `number` stays one type; roots return the exact answer when one exists |
| machinery | none new — the representation already discriminates integer / rational / promoted; *inexact* is one more case of a tag already being read |
| the honest guarantee | *exact wherever exactness exists; where it is not, the value says so.* The stronger version was never keepable |
| what `fast` now means | **speed, and only speed** — an unboxed `double[]` column outside the tag dispatch. Not "this happens to be approximate" |
| visibility | the **ellipsis rule already does it** — an inexact value's rendering is always lossy. Hover distinguishes *exact value, lossy display* from *approximate value* |
| the residual | equality across exactness will surprise. The compiler sees it **statically**, so it is a diagnostic, not a mystery |
| per-column promotion | agreed — plus **demotion via an O(1) count** of over-wide elements, or the column is a ratchet in a program that never restarts |
| and | leave the tag room for **three** states — unboxed, unboxed-plus-overflow-table, promoted — so the measurement can decide later without a rewrite |
| but | per-column fixes **storage**, not **growth**. The smoothing cell is a scalar; it still needs the watchdog |
