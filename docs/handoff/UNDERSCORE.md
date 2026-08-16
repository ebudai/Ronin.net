# `_` — yes, a stand-in, and that changes the answer

> **Ledger** — `[V]` `_` — yes, a stand-in, and that changes the answer
> supersedes: EMPTYBRACKETS §2
> superseded by: none

Budai: *"is that just a stand-in for any name?"* Yes — and asking it is what
makes the previous note wrong. Correcting before anyone implements from it.

---

## 1. What `_` is in the registry

`send (message) to (recipient)` renders as `send (_) to (_)`. The registry is
about **pattern shape**, so the renderer deliberately drops the parameter names.
`_` marks "a hole was here; what it was called is not this file's business."

That has a consequence I missed: **the rendering discards information on
purpose, so a declaration can never round-trip through it.** `Parse(Render(d))`
cannot recover `d` — the names are gone.

So the property I asked for in `SWEEP-ITEMS.md` §1 was stated on the wrong
object. It should be:

> `Parse(Render(p)) == p` where **`p` is a pattern** — a shape — and equality is
> pattern equality, not declaration equality.

Which is achievable, and is what the test should assert. A declaration
round-trip is not a thing that can exist and should not be asked for.

## 2. Which makes the simple answer the right one

`EMPTY-BRACKETS.md` proposed `(_)` as *source syntax* for an unnamed hole. Given
§1, that is not needed for the round-trip, and it is not needed for anything
else anyone has asked for. So:

> **`(_)` is notation, not syntax.** `Pattern.Parse` is a parser for pattern
> notation — used by the registry, tests and tooling — and it is a different
> grammar from the source language. User declarations always name their holes.

No language change at all. `_` never enters Ronin. The bug is still a bug —
`Pattern.Parse` must round-trip or reject, per the corrected property above —
but it is fixed entirely inside the notation parser.

This also retires the question in my previous draft about whether `_` should be
an identifier character. It does not arise: the source language never sees one.

## 3. If `(_)` ever does become source syntax, it must be special

Keeping the analysis, because it is the part that would bite:

`_` could not be "just an ordinary identifier". Two holes spelled the same way —

```
send (_) to (_)
```

— would be two parameters named `_`, and `SCOPING.md` bans shadowing precisely
so the symbol table can be a flat merge, with parameters in it: *"a parameter
can't be called `name` inside a type that has a `var name`."* They would
collide.

So it would have to be a **hole marker that binds nothing** and therefore
repeats freely, as in Rust, Go and OCaml — legal only in hole position in a
declaration, a finding anywhere else. That is a real special case, and it should
be paid for only when someone actually wants an unnamed parameter. Nobody has.

## 4. Where `_` appears, so notation stops being mistaken for syntax

| where | what it is |
|---|---|
| `(_)` in registry output, `reserved-words.txt` | **pattern notation** — a stand-in for an elided name |
| `<_>`, `{_}` in probe output | **my display notation only** — see `SWEEP-ITEMS.md` §0 |
| bare `_` in `patterns.txt` | **my seed-file format** — not source, not compiler output |
| anywhere in Ronin source | nothing. It is not part of the language |

Three of the four rows are notation that reads like syntax, and two of them have
now produced findings — `Pattern.Parse` silently misreading `<_>`, and this
question. The table belongs in the handoff folder.

The general lesson is cheap and I keep relearning it: **notation that resembles
source will eventually be handed to a parser.** Either make it parseable and
round-tripping, or make it visibly not-source — words in a column, not brackets
and underscores.
