# `time to live` — yes, it can be legal, and I have been over-weighting one word

Measured it rather than defending the rule, and two things I have been assuming
are false. The short version: **interior glue is not dangerous on its own**, and
the residual hazard is not the reading but the **edit** — which has an
instrument already designed for it.

One honest gap at the end, and a scheduling note that makes this safe to defer.

---

## 1. Interior glue needs a shorter sibling pattern

```
  send (_) to (_)  alone
      send time to live        OK 3 -> OK 3   unchanged
      send message to server   OK 3 -> OK 3   unchanged

  send (_) to (_) | send (_)
      send time to live        OK 3 -> OK 2   CAPTURE
      send message to server   OK 3 -> OK 3   unchanged
```

Without a `send (_)` there is nothing for `time to live` to be an **argument
of**, so the only reading is the two-argument call and the name is harmless.
The hazard has a condition, and it is a condition over the pattern table —
the same species of conditionality already accepted for R7b's article.

And note the second row of each block. **`send message to server` does not care
that `time to live` exists.** The collision was never "`to` is unusable inside
names" — it is "this exact phrase is now also a name". I have been describing a
much larger blast radius than the one that exists.

## 2. "Silent" was doing more work in my argument than it earns

Where the sibling does exist, the capture is real. But Ronin **already requires
the reader to know the symbol table** — that is what spaces-in-names means, and
`base price` versus `base` `price` is settled the same way. A reader who knows
`time to live` is in scope reads `send time to live` exactly as minimum lookup
does.

So the reading is not wrong. And both readings stay reachable:

```
  with the name    send time to live           OK 2  send «time to live»
                   send ( time ) to ( live )   OK 5  send ⟨«time»⟩ to ⟨«live»⟩
                   send ( time to live )       OK 3  send ⟨«time to live»⟩
```

The price Budai offered — brackets at the ambiguous site — is available and
sufficient in both directions.

## 3. What is actually left is the edit, and we already designed the instrument

The residual hazard is someone adding `var time to live` in an outer scope and a
statement written *earlier* changing meaning with no diagnostic.

`MODULE-MERGE.md` §4 specified exactly this shape one level up:

> an **import** may not change the reading of any statement already in the
> importing module

Applied to declarations it is the same check, same machinery:

> a **declaration** may not change the reading of any statement already in its
> scope

```
  declaring «time to live» -- checking 4 statements in scope:
      REJECT  «send time to live»
              was: send «time» to «live»
              now: send «time to live»
      ok      «send message to server»
      ok      «send time»
      ok      «send ( time ) to ( live )»
```

So `time to live` is **declarable**, and refused only in a scope that already
contains a statement it would re-read — with that statement named, so the fix is
a bracket on one line rather than a rename of the variable. That is the trade
Budai asked for, and it is strictly better than the blanket refusal because the
diagnostic points at the actual conflict.

Cost: per-declaration over the scope, and only statements containing the name's
token run need looking at. A token index makes it cheap, and the always-running
environment is why it is affordable at all.

## 4. The gap, stated plainly

The differential check protects code that **already exists**. It does not
protect code written **afterwards**:

```ronin
var time to live = 30;
send time to live;        // means «send «time to live»», not the two-arg call
```

No statement changed, so nothing fires. The blanket rule catches this; the
differential check does not.

Whether that matters depends on whether you think the second line is *wrong*.
Given §2 — the reader knows the table, and the name is right there two lines
up — I think it reads correctly and the person who wanted the call writes
brackets. But it is a real difference between the two rules and it is Budai's
call, not a detail.

## 5. What stays blanket regardless

```
  send to to to to   + «to» and «to to»   TIE -> ERROR
```

The all-glue clause fires with **no sibling pattern**: the two readings are two
placements of the same literal, so there is no shorter form to blame and no edit
to point at. That one stays unconditional — and it is a useful demonstration
that "make it conditional" is not a general answer, only the right answer where
the hazard actually has a condition.

## 6. Scheduling — and this is the part that makes it safe

**R5′ as landed is sound. It is conservative, not wrong.** Every name it refuses
today would be admitted under the proposal, and none the other way round. So:

> Narrowing a refusal is always backward-compatible. Widening one is not.

Which means this does **not** have to land now, and nothing is lost by waiting —
unlike the glue-registry closing date, where the window genuinely shuts. He
should finish the slice as built, take the audit, and treat the differential
check as its own piece of work when the module-boundary version is being built
anyway, since it is the same machinery twice.

If Budai wants `time to live` sooner than that, the cheap interim is §1's
condition alone: **refuse interior glue only when a sibling pattern exists.**
That is a pattern-table check, needs no per-scope machinery, and it recovers
`time to live` in every registry that has no `send (_)`. It does not recover it
where the sibling does exist — the full differential check is what does that.

## 7. Summary

| | |
|---|---|
| can `time to live` be legal | **yes** |
| why I thought not | I described the blast radius as "`to` is unusable in names". Measured, it is "this exact phrase is also a name" |
| the hazard's condition | a **shorter sibling pattern** must exist; `send (_) to (_)` alone is harmless |
| "silent capture" | over-weighted — the reader already has to know the table, and both readings bracket cleanly |
| what is actually left | the **edit**: a later declaration re-reading earlier code |
| the fix | `MODULE-MERGE.md` §4's differential check, applied to declarations instead of imports |
| the gap | it does not protect code written **after** the declaration. Budai's call whether that matters |
| stays blanket | the all-glue clause — no sibling needed, no edit to blame |
| when | **not now.** R5′ as landed is conservative-but-sound, and narrowing a refusal is always backward-compatible |

Probe: `time_to_live.py`.
