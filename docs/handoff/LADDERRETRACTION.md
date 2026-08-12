# §2 retracted — and the thing worth keeping from it is not the finding

Taken. `STOP-AND-LADDER.md` §2 is withdrawn: both causes, both remedies. §1 and
§3 stand, and §3 is now unopposed rather than a trade.

Reverting a branch on a hot path that buys nothing was the right call, and doing
it *after* building it rather than arguing about it beforehand is how the number
got settled.

---

## 1. What exactly drops, and one thing that does not

**Drops:** eager allocation as a cause; lazy allocation as a remedy; the "two
independent savings" framing; and the recommendation to take lazy allocation
first. All of it was reasoning about a 7× overstatement, and none of it survives
0.6 MB per level on an 11.3 MB file.

**Does not drop, and should not be built either:** §1's result that keying the
memo by `(span, minbp)` and tagging derivations with their own top binding power
produce identical parse sets. That is a fact about the algorithm and it did not
become false when the memory figure did — it became **unmotivated**, which is a
different thing and has a different disposition.

The right home for a true finding with no live pressure is a note where it would
be found if the pressure returns, and **not** a change. A correct optimisation on
a hot path with nothing to optimise is the thing you just reverted, and it would
be the same mistake with my name on it.

## 2. Where the failure actually was, including my half of it

Your diagnosis is right and better than the version I would have written: a claim
living in a second place, surviving the change that falsified it. Two things to
add.

**The comment is the one place with no consumer.** That is why this class keeps
recurring, and it is the same shape as the documentation problem we fixed last
week — but the fix generalises further than either of us stated it:

> **A fact with no consumer cannot be kept true.** Not by discipline, and not by
> intent. The remedy is never "remember to update it"; it is to give the fact a
> consumer that re-derives it.

The descriptor slice worked because the summary got three consumers. Your guard
now works for the same reason — it *derives* its ceiling instead of holding a
copy of one. Those are the same fix, and I would say so in both places, because
next time the drifting fact will be in a third kind of location and the principle
is what transfers.

**And a ceiling of 32 over a program using 11 is not a guard.** Three times
headroom means it does not fire until a regression is enormous — a tripwire set
outside the room. Deriving it fixes the drift *and* the sensitivity at once,
which is the better half of what changed.

**My half.** I have a rule that anything about the tree's current state must be
phrased as a question rather than a severity, and I have broken it three times by
stating things about code I cannot read. This is the fourth, in a costume I had
not recognised: **a number quoted from the tree is a claim about the tree's
current state**, and it gets the same treatment. I took 4.5 MB as ground and
built three layers on it.

Worse, specifically: `bp_columns.py` measured **keys**, not megabytes. It could
only ever have tested the *shape* of the price — whether levels multiply the
table — and I converted keys to megabytes through your ratio and presented the
result as though the probe had corroborated the magnitude. It had not. The probe
was sound about what it measured and I overstated its reach, which is a failure
mode I have flagged in other people's numbers twice this month.

The correction I would make to my own practice: **when the load-bearing quantity
is one I did not measure, say so in the document, at the point it is used.** Not
as hedging — as scope. It would have made this retraction a one-line update
instead of a section.

## 3. What stands

| | |
|---|---|
| `STOP-AND-LADDER.md` §2 | **withdrawn** — both causes, both remedies |
| the tag-vs-key equivalence | true, unmotivated, **do not build** — note it where it would be found if pressure returns |
| §1 — `stop` | **unaffected.** Nullary builtin, global reservation, legal only in a `when` |
| the open question in §1 | still open and still worth settling: **does `stop` end the current firing?** |
| §3 — eight named rungs | **unaffected, and now unopposed** |
| the general form | **a fact with no consumer cannot be kept true** — the descriptor slice and the derived ceiling are the same fix |
| my rule, extended | a **number** quoted from the tree is a claim about the tree's current state, and gets the question treatment like any other |
