# `wait until` when the condition is already true — decided: proceed

> **Ledger** — `[V]` `wait until` when the condition is already true — decided: proceed
> supersedes: not yet checked
> superseded by: not yet checked

And on the injected names: the spellings are the smaller half of that question.
There is a prerequisite underneath them.

---

# 1. `wait until B` with B already true proceeds in the same step

He is right that both readings are defensible and that test 9 does not settle
it. **Level, not edge.** His implementation is correct and should stay.

## Why, in order of weight

**The failure modes are asymmetric, and only one of them is silent.**

```
when order placed { reserve stock; wait until payment cleared; ship }
```

A prepaid order has `payment cleared` already true. Under edge semantics it
never ships — no error, no diagnostic, nothing in the log. The symptom is
"sometimes orders just don't go out", and finding it requires reasoning about a
value's history.

The mirror case exists:

```
when button pressed { start animation; wait until animation done; hide }
```

with `animation done` still high from the previous run, level semantics hides
too early. But that fires *immediately and visibly*, on the first run, and the
program can fix it by clearing the flag. The edge failure cannot be fixed
inside the program at all without manufacturing a fake transition.

For a language meant to let a beginner build something that works, a permanent
silent hang is a much worse default than an over-eager fire.

**The desugaring already produces it, and not by accident.** `flag AND B` with
becomes-true semantics makes arming itself the rising edge. That is what falls
out of expressing a wait as a `when` over a conjunction. Edge semantics would
require *adding* machinery — remembering B's value at arming time and comparing
— which is a sign it is the less natural reading of the design we chose.

**It is consistent with the model.** Ronin is a graph over values, not a stream
of events. `when` is the one edge-triggered construct and it is edge-triggered
on *its own* condition. `wait until B` is a guard on a continuation, not a
second trigger, and guards are level. If an author genuinely wants a second
trigger, the honest spelling is a second `when` — which is what they would
write anyway, and it says what it does.

**And English agrees.** You do not wait for the kettle if it has already
boiled.

## Do not add an edge spelling

`wait for B` beside `wait until B` would be two words differing subtly in a way
that is invisible at the call site — the same objection as Rust's trailing
semicolon. One form. An author who wants an edge writes `wait until not B` then
`wait until B`, which is explicit and readable, or uses a second `when`.

## Tests §6 needs, since test 9 does not cover this

| # | case | expect |
|---|---|---|
| 9a | B already true when A fires | `x` and `y` in the **same step** |
| 9b | B false at arming, later true | `y` in the later step |
| 9c | `x` sets B **false** in the same step | waits — the guard sees B after `x`, not before |
| 9d | `wait until true` | degenerate no-op, same step |

9c is the one worth writing carefully. It is the case that tells you whether
the guard is evaluated at the end of the step or against the pre-step value,
and both are plausible implementations of "level".

---

# 2. The injected names — one objection, one prerequisite, then spelling

## The objection: `in flight` uses `in`

`«when a in flight 1»` contains `in`, which is the single word in this language
most likely to become glue again. It was glue until the pinned-hole change, and
the loop is the construct most likely to be respelled. A generated name that
breaks if `in` is re-reserved is a trap set for later — and it would fire on
every `wait until` in every program at once.

Injected names should avoid words that are plausible future glue. Words already
in the **protected** set are the safe ones, because no pattern may use them as
glue by rule: `old`, `index`, `of`.

## The prerequisite: can a `when` be named?

This is the part worth settling before the spellings. All three generated names
are derived from *the `when`*, so the language needs a stable way to refer to
one. Today a `when` is identified by its condition, and that gives:

```
«when health <= 0 in flight 1»
```

which is ugly, and worse, **unstable** — edit the condition and every generated
name changes, so a diagnostic or a `Fired` entry that referred to one goes
stale. It also means two `when`s with the same condition collide.

So: **`when`s should be nameable**, with generated names derived from the name.
Something like

```
when low health : health <= 0 { … }
```

with a stable fallback for anonymous ones — declaration order within the
module, not a span, so that editing an earlier line does not renumber
everything downstream.

I am not attached to that spelling. The point is that a nameable `when` is a
prerequisite for user-visible generated names, and it is cheaper to add now
than after `Fired` entries and diagnostics are keyed on condition text.

## Then the spellings

Given a name, follow `index of X` — qualifier first, subject after, using the
protected connective:

```
wait 1 of low health        the flag for the first wait
resuming low health         the continuation «when»
waiting of low health       true while any wait is armed
```

Two properties that matter more than the words: **one scheme applied by rule,
not three ad-hoc spellings**, and the set is **generated and checked in**, the
way the glue registry is — so nobody discovers a collision by hitting it. The
same file that lists protected words should list the injection shapes, since
they are the two halves of the same rule.
