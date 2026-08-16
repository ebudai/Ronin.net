# REAUDIT56 — two findings that turn on a decision

> **Ledger** — `[R]` REAUDIT56 — two findings that turn on a decision
> supersedes: not yet checked
> superseded by: not yet checked

Findings 1 (patterned-datum crash) and 3 (overload-wide uniqueness omitting
ancillary delegates) are implemented and pushed. Finding 2 (a function's own
signature cannot see its body-local types) is a mechanical, if large, change and is
being implemented separately — no decision needed there.

Findings **4** and **5** each carry a fork the auditor left open, and both touch
decisions already on record, so they are relayed rather than picked.

---

## Finding 4 — an inference variable's requirement handle

**The bug is not in question.** `Sort.Variable` is equal and hashes by `Identity`
alone, but every construction makes a fresh mutable `HashSet<Pattern>`. So two
`Variable(7)` compare equal yet answer differently about their interface — a term
reconstructed, copied, or met through another path silently loses accumulated
requirements. Something must change; equal values may not own independent mutable
state.

Two things are undecided.

**Q4a — how equal variables come to share state.**

| option | shape | cost |
|---|---|---|
| **shared cell by identity** | a lookup `identity → requirement set`, every `Variable(n)` reading the one set | a map keyed by identity — the shape `CHECKER-SCOPING-RULINGS` Q1 and your finding-4 note steered *away* from ("a slot on the variable, not an external map") |
| **enforced object uniqueness** | one `Variable` instance per identity, minted through a factory/cache; equality becomes reference identity | `new Sort.Variable(n)` can no longer be free; construction sites route through the factory |

The finding-4 note asked for "a slot ON the variable." Enforced uniqueness keeps
that phrase literally (the slot sits on the single object). A shared cell is the
external map the note named as the thing to avoid — but is simpler and needs no
factory. Which do you want?

**Q4b — what the slot holds.** `GENERICS-II` §5 defines a requirement as *a pattern
resolving for a tuple of types*, with provenance (the `max` example is one operation
over a pair of variables). `ISet<Pattern>` records neither the operand relationships
nor the source/propagation chain the call-site diagnostic needs. But your finding-4
note said to add the slot *"now, empty, without the constraint machinery behind
it."* So there is a genuine tension:

- **keep it minimal** — honour "no machinery yet"; the slot exists and is shared
  (Q4a), element type stays a stand-in until the constraint pass lands; or
- **shape the value now** — define a requirement value carrying (pattern,
  participating type terms, provenance) so the handle is the final shape the
  constraint pass fills, not a placeholder it will rewrite.

The audit wants the second; your prior ruling implies the first. Your call.

---

## Finding 5 — every pathless source is one module

`Compilation.Scope` roots the container at `[Source.Path]`. `SourceText.Path` is
deliberately optional — its contract says editor buffers and tests may have none —
so two independent pathless compilations both produce `Named([null], "token")` and
compare **equal**. The path-bearing case you ruled on is fixed; this is the pathless
buffer the source abstraction still promises to support.

**Q5a — reject or distinguish.**

- **reject pathless compilation** — refuse a `SourceText` with no path, and amend the
  `SourceText` contract that currently promises pathless buffers are valid; or
- **give a pathless source a distinct structural module token** — keep the contract,
  make two pathless buffers two modules.

Rejecting contradicts a written contract editor integration relies on, so the lean
is to distinguish — but the contract is yours to change if you prefer.

**Q5b — if distinguished, how the token is represented.** The container is
`IReadOnlyList<string>` and the module segment is the path string. A pathless source
has no structural distinguisher but its own object identity, so a distinct token has
to come from that (a per-`SourceText` identity). The `§3` prohibition still binds:
**do not render a synthetic token into a string and parse it back.** Two shapes fit:

- **an opaque per-source identity string** the module segment carries when there is
  no path — never parsed as a path, only ever compared; or
- **a module-identity type** replacing the bare `string` segment at the root — a path
  *or* a source identity, so the "never parse it back" rule holds by construction
  rather than by discipline.

The first is a smaller change; the second makes the prohibition structural. Which
fits how you want module identity to read?

---

## Summary

| | question | lean |
|---|---|---|
| **4a** | shared cell vs enforced object uniqueness | none — the note's wording points at uniqueness, simplicity points at the cell |
| **4b** | shape the requirement value now, or keep it minimal | none — the audit and your prior ruling disagree |
| **5a** | reject pathless, or distinguish it | distinguish (rejecting breaks the `SourceText` contract) |
| **5b** | opaque identity string vs a module-identity type | none — smaller change vs structural guarantee |
