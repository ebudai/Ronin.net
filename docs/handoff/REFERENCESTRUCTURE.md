# Where the reference sentences go — and the `Scope.Invoke` check

> **Ledger** — `[V]` Where the reference sentences go — and the `Scope.Invoke` check
> supersedes: none
> superseded by: none

Two things to answer. §6 is the real one, and "tell me where they belong and I
will write them; I would rather ask than invent a documentation structure" is
exactly right — the structure decides whether this stays true in six months, and
that is a design question rather than a writing one.

`disarm` correction taken. `Graph.Stop` removes the node, "disarm" reads as
reversible, and nothing can re-arm a `when`. His sentences are better than mine
and I would ship them as written.

---

## 1. Not a documentation structure — a field on the declaration

The answer to "where do these sentences live" is **not a directory**. It is:

> **The reference text is a field on the builtin's declaration, and
> `docs/reference` is generated from the table.**

Three reasons, in order of weight.

**a. This project's failure mode is documentation drifting from the
implementation, and it has happened four times.** `nothing` was decided in
conversation and never written. `INJECTED-DEDUP.md` kept a superseded table after
the correction. `SCOPING.md`'s shadow rules went dead without being marked. And
his own §5 — *"answered in conversation and not written down, which is my mistake
and exactly the drift this channel exists to stop."* A hand-maintained
`docs/reference/return.md` is a fifth instance waiting to happen. Generation is
the structural fix, and it is the only one that does not depend on somebody
remembering.

**b. The always-running IDE needs this text at the cursor.** Hover on `return`
should show that sentence; so should completion. If the text lives in a markdown
file, the editor needs a second copy or a parser for prose. If it lives on the
declaration, **one source feeds three consumers** — hover, completion, and the
generated reference — and they cannot disagree.

That is not a nice-to-have here. `DONT-DO-THAT.md` and `AMBIGUITY-AS-ERROR.md`
both lean on the editor explaining things at the point of confusion; this is the
same text those need.

**c. It is the move we have now made three times, and it has been right each
time.** Don't fork the structure to hold a distinction — carry it as a field.
Overloads became a candidate set rather than split derivations. Types and values
became one table with a *kind* rather than two tables. Documentation is the same
shape: one table with a *description*, not a parallel tree that has to be kept in
step by hand.

## 2. What the field looks like

Small enough that nobody skips it, structured enough to be checked:

```
  summary     one sentence. What it does.
  forms       the spellings, one per arity: «return (the answer)» / «return»
  legal in    the positional rule -- a function that answers / an action /
              a «when» body
  see also    a list of ENTRY NAMES, not prose
```

Two properties worth having, both cheap and both only possible because it is a
field:

**`see also` is checked.** An unknown name is a build error. The whole reason
these two sentences exist is that each names the other; a cross-reference that
can rot is a cross-reference that will. This also means the editor can render
them as links without parsing anything.

**A builtin with no `summary` fails the build.** That is how "docs/spec has no
per-keyword reference" stops being possible. It is not a documentation policy,
it is a missing required field, and it is enforced by the same mechanism that
enforces every other one.

And the payoff from `FIVE-RULINGS.md` §4 shows up here too: **the reserved-words
registry and the reference come out of the same table**, because there is only
one table. Two generated artefacts, one source, no second namespace to keep in
step.

## 3. What that leaves for the two directories

```
  docs/guide/       hand-written prose. Tutorial, orientation, worked examples.
                    Stays a README until it needs not to be. Nothing here is
                    generated, and it is allowed to be selective.

  docs/reference/   GENERATED. One page per entry, plus an index. A banner at
                    the top saying so, and never edited by hand -- an edit here
                    is lost on the next build, which is a property to advertise
                    rather than a trap to discover.
```

Guide answers *how do I do X*; reference answers *what exactly is Y*. Keeping the
first hand-written is deliberate — a generated tutorial is a bad tutorial, and
that is the half where prose earns its keep.

## 4. So, concretely, for `return` and `stop`

Put his sentences in the `summary`/`legal in` fields of the two `Builtins`
entries, with `see also` pointing at each other. They then appear in the
generated reference, in hover, and in completion, from one place.

The diagnostic string stays where it is. It is a *third* consumer with a
different job — the reference explains the feature, the diagnostic explains this
program — and trying to generate one from the other makes both worse.

## 5. `Scope.Invoke` — keep it, demote it, and change what it says

Answer received, thank you — and the important half is his: **it cannot fire from
real source today**, because `Overloaded` refuses the declaration at compile
time. So `OVERLOADS.md` §4a's fear is real about the *location* and not yet real
about the *exposure*. That is a much better position than the question implied.

Ruling on what happens when the compile-time filter lands:

> **Keep it, as an invariant assertion, and rewrite the message to say it is a
> compiler bug.**

Not belt-and-braces, and not dead:

- If the compile-time filter is correct, this can never fire — so keeping it
  costs one comparison and buys the cheapest possible detection of a filter bug.
  Deleting it means a filter bug produces a **wrong call, silently**, which is
  the failure mode this project has spent the most effort eliminating.
- *"It is the only place that currently names the condition"* is an argument for
  moving the naming, not for keeping the check. Once the filter lands, the
  condition is named in the **compile-time diagnostic**, which is where a user
  can actually meet it — and that diagnostic carries the `(x => Text)` ascription
  repair as a selectable suggestion.

**But the message must change, and this is the part I would not skip.**
`«{pattern}» is ambiguous after type filtering` is a *user-facing* sentence
sitting at a place no user can reach. If it ever fires it is a compiler defect,
not a program defect, and a user-facing string guarantees that one day someone
reads it and has no action available. Something like *"internal: N candidates
survived the compile-time filter for «pattern» — this is a compiler bug"* costs
nothing and tells the truth.

The general form, since this is the second time it has come up:

> **A check that cannot fire from source is an assertion, and an assertion's
> message is addressed to the compiler's authors.** Keeping the check is cheap;
> keeping the *wrong audience* is not.

## 6. Summary

| | |
|---|---|
| `disarm` | **his correction is right** — `Graph.Stop` removes the node; ship his two sentences verbatim |
| where the sentences go | **a field on the declaration**, not a file |
| `docs/reference` | **generated** from the table, banner saying so, never hand-edited |
| `docs/guide` | stays hand-written prose; a generated tutorial is a bad tutorial |
| the field | `summary`, `forms`, `legal in`, `see also` |
| `see also` | **checked** — unknown name is a build error |
| missing `summary` | **build error** — that is how "no per-keyword reference" stops being possible |
| why not a directory | four documented instances of docs drifting from code, including his own §5 |
| the other payoff | the reserved-words registry generates from the same table — `FIVE-RULINGS.md` §4 again |
| `Scope.Invoke` | **keep, as an invariant assertion** |
| its message | **rewrite** — it is addressed to the wrong audience for a check no user can reach |
