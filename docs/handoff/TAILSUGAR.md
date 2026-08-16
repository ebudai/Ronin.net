# No action marker, and `{ x }` is `{ return x; }`

> **Ledger** — `[V]` No action marker, and `{ x }` is `{ return x; }` — ruled (the `[V/R]` resolves to `[V]`). The checker rule it implies is a recommendation.
> supersedes: none
> superseded by: none

Both taken. And the sugar has a better justification than the one you gave for
it — it is a **correction**, not an addition.

---

## 1. The gap you verified — ruled, and no marker

Your finding is right: there is no `action` keyword, an omitted `=>` leaves
`Returns` null, and `FIVE-RULINGS` §2b's claim that *"the declaration form
already says whether a thing is an action or a function"* is simply false.
**§2b is corrected.**

But it does not need a marker, and the reason is a ruling already taken.

> **A body with no return is an action, decided by its body.**
> `function f (x) { }` is an action. Pathological, and ruled rather than refused.

That leaves the declaration line silent about whether the thing answers, which
is a readability question — and `RETURN-AND-LITERALS` §2 already answered that
one for the identical case of an inferred return type being invisible at the call
site: **not a rule, the editor displays the inferred type inline**, the same way
implicit brackets show on hover. Same problem, same answer, and it costs no
reserved word, no third declaration form, and no marker that can disagree with
the body it labels.

*(I did reach for an `action` keyword first. It was the wrong instinct for the
reason above — a readability question answered with syntax after already ruling
that the editor answers it — and Budai withdrew it before it reached you.)*

## 2. The sugar is fixing an inconsistency, not adding a convenience

The argument to put in the guide is not brevity:

> `if c { a } otherwise { b }` is an **expression** (`IF-AS-EXPRESSION.md`), so
> `{ a }` in that position already means *the value a*. Without this ruling,
> `{ x }` means one thing inside an `if` and a different thing inside a function
> body.

That near-miss is exactly what a reader has to hold in their head and eventually
gets wrong. The sugar makes a block mean one thing everywhere it appears.

## 3. The determination is total — enumerated

Every last-statement shape against every block context, zero undetermined:

```
  FUNCTION BODY
    «x»  «x + 1»  «if c { 1 } otherwise { 2 }»    sugars -> return that value
    «print x» -- an action call                       no -> the body ends, ACTION
    «return x» / «return»                             no -> already a return site
    a declaration / an empty block                    no -> ACTION
```

And it is total for a reason rather than by enumeration: **the action type is not
admissible in a value position** (`FIVE-RULINGS` §2b), so `print x` cannot be
returned and the sugar never reaches it. No extra rule, no guess about what the
author meant.

## 4. Three guards, and one style line

**a. Only the FINAL statement sugars.** A bare value expression earlier in a body
computes something and throws it away. Silence there means someone writes `x`
mid-body intending a return and gets nothing — **ephemeral warning**, the class
proposed for the single-variable case.

**b. A `when` body does not sugar.** A `when` never answers, so a trailing value
there is discarded — same warning. Without this guard the sugar produces
`return x` inside a `when`, which `RETURN-AND-LITERALS` §1b refuses, and the
author gets a message about a `return` they did not write. That is the worst
diagnostic shape there is.

**c. A trailing terminator does not disable it.** `{ x; }` sugars exactly as
`{ x }` does. `Aggregate.Parsed` already treats a trailing separator as elision,
so this needs saying rather than building — but it needs saying, or it gets
discovered.

**Style, one line for the guide:**

> **The sugar is for the answer; `return` is for an early exit.**

That makes the two forms non-competing rather than two spellings of one thing,
which is what stops an idiom war before it starts.

## 5. What it costs, and one thing it improves

Nothing new is reserved and nothing existing moves. The return-site collection in
`RETURN-AND-LITERALS` §1c is unchanged — a sugared tail **is** a return site and
is collected as one, so the legality rule and the inference still read the same
set.

And recursion reads better for it, which is not nothing given
`RECURSIVE-RETURN` was about exactly these bodies:

```
  function factorial (n) {
      if n <= 1 { 1 } otherwise { n * factorial (n - 1) }
  }
```

Two return sites, one independent of the recursion, base-case-first solves it,
and no `return` appears anywhere.

## 6. Summary

| | |
|---|---|
| the gap you verified | **real, and `FIVE-RULINGS` §2b is corrected** |
| the ruling | **a body with no return is an action**, decided by its body. No marker |
| why no marker | the declaration line staying silent is a readability question, and `RETURN-AND-LITERALS` §2 already answered that one — the **editor** shows the inferred type inline |
| `{ x }` ≡ `{ return x; }` | **taken** |
| the real argument for it | `{ a }` in an `if` branch already means *the value a*. This makes a block mean one thing everywhere |
| is it total? | **yes, 0 undetermined** — because the action type is inadmissible in value position, so `print x` cannot sugar |
| guard 1 | only the **final** statement sugars; an earlier bare value is an ephemeral warning |
| guard 2 | a **`when` body does not sugar** — otherwise the author gets an error about a `return` they never wrote |
| guard 3 | `{ x; }` sugars too — trailing terminator is elision |
| style | **the sugar is for the answer, `return` is for an early exit** |
| unchanged | return-site collection, the exit-flavour rule, the recursion rule |

Probe: `tail_sugar.py`.
