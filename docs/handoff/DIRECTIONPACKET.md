# Direction packet — writers, and the counterexample hunt I asked for

> **Ledger** — `[R]` Direction packet — writers, and the counterexample hunt I asked for
> supersedes: not yet checked
> superseded by: not yet checked

Two items he flagged. The first I agree with and can sharpen. The second was
assigned to nobody, so I did it.

---

## 1. Chain segments must be one writer — confirmed, and one trap alongside

He is right that §5's check does not currently hold: `Cascades.Writers` reports
*"«charging» is written by 2 whens"* for a program that is one `when` in source.
That makes the suppression idiom unwritable, which makes the §3 advice
unfollowable, so it is a prerequisite rather than a nicety.

**The trap: merge writer identity, not node identity.** His own earlier
provenance showed that merging two bodies under one identity turns a legal chain
into a false ring — `Cascades` reported *"«when ready» → «when ready» is a
cycle"* for a program where one body writes what the other reads. That is
exactly what a chain is: segment 1 writes what segment 2 reads.

So the two analyses want different groupings of the same nodes:

| analysis | grouping | why |
|---|---|---|
| single-writer | segments of a chain are **one** writer | the author wrote one `when`; ownership is a source-level property |
| cascade / SCC | segments are **distinct** nodes with a real edge | the edge is the chain, and it is not a cycle |

Anything that unifies them will break one of the two. Worth a test for each
direction, since the failure in one is silent and the failure in the other is a
false diagnostic on a correct program.

---

## 2. The counterexample hunt — done, and it found something

I asked for a deliberate search for a program where counting is worse than
restart, excluding smuggled data. There is one, and it is a whole family:

**Supersede semantics — where a new trigger should cancel the pending one.**

```ronin
when activity { wait until 5 minutes; save }
```

Idle-autosave. Under restart, activity resets the countdown and you save five
minutes after the *last* activity. Under counting, ten activities give you ten
independent five-minute timers and ten saves. The same shape is
search-as-you-type debounce, screensaver idle, alarm re-arming, and every
"reset the countdown" pattern — and it is at least as common as the
one-run-per-event family that counting gets right.

Worse, the obvious workaround is *ignore*, not restart:

```ronin
when activity and not pending { pending = true; wait until 5 minutes; pending = false; save }
```

which saves five minutes after the **first** activity. Silently the wrong one.

### But it does not force a policy back

It forces a *documentation obligation* instead, because the supersede family
does not want a chain at all. It wants a deadline as a value:

```ronin
when activity          { save at = now + 5 minutes }
when now >= save at    { save }
```

That is restart semantics exactly, with no chain, no policy, no generated name —
and it is the more idiomatic formulation for a graph over values. The deadline
*is* the state, so superseding is just writing to it.

So the selection rule, which should be in the guide next to `wait until`:

> **A chain is for "each occurrence gets its own run". A deadline value is for
> "the latest occurrence supersedes."** If a new trigger should cancel what is
> pending, you do not want a chain.

There is a second reason to prefer the value form where it applies, and it is
not aesthetic: **a chain can accumulate and a value cannot.** The deadline
formulation has exactly one pending save no matter how fast activity fires,
structurally, with no bound to tune and no diagnostic to hit.

### What this does to `CHAIN-ACTIVATIONS.md` §3

It stands, with a caveat I should have written the first time: "delete the
policy" is correct, but only because **one of the two behaviours moves out of
chains entirely.** I presented counting as strictly better and it is not — it is
better for the cases chains are for, and the cases it is worse for are cases
that should not be chains. That is a narrower and more honest claim, and it puts
an obligation on the guide that the earlier version hid.

I would not have found this by reasoning about the mechanism; it came from
trying to break it with ordinary programs, which is the thing he was right to
say nobody had done.

---

## 3. What I would put in the next packet

1. Chain segments key as one writer; nodes stay distinct. Test both directions.
2. `finish` is dropped in favour of `return`, which already means "leave this
   body and do not do the rest" — and because arming the next segment only
   happens *at* a wait, ending the activation falls out. `stop` keeps its
   existing meaning: disarm the `when`. Worth confirming `return` is currently
   legal in a `when` body before relying on it.
3. Counting stands as the only activation policy.
4. The guide gains the chain-versus-deadline selection rule above, and
   `wait until`'s documentation points at it. This is the deliverable that
   stops the supersede family being written as a chain and getting ten timers.
5. Still open and still nobody's: whether one-activation-per-round makes the
   existing runaway detector sufficient for accumulation. That is a claim about
   a detector I have not read, and it is the last unverified load-bearing
   assumption in the chain design.
