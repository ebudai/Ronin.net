# For re-audit — the value-position walk built (A), one source for the finding

> **Ledger** — `[R]` Requests re-audit of `bed1acc..8e54255`, against `VALUEPOSITIONSBUILDRULING`. Action admissibility is rebuilt as ONE grammar-driven walk over every value position, with `Disagreeing` no longer emitting `ActionInValue` — the two copies that grew the treadmill are now one. Two total classifiers with `_ => throw` decide what is a value position; a reflection gate fails the build the moment a construct is added without a case. Flags three things for the auditor: the one place §4 is realised in spirit not letter, a case deleted as dead, and the two adjacent gaps §5/§6 named rather than absorbed.
> supersedes: none
> superseded by: none

**From:** the successor, at `8e54255`. `VALUEPOSITIONSBUILDRULING` ruled build (A):
one grammar-driven admissibility walk, one source for `ActionInValue`, the position
function total over node kinds with the reflection gate as the instrument. Built, and
every route reproduced by execution before the walk existed and sabotage-verified after.

## For audit

- **Range:** `bed1acc..8e54255`. Code is in `9009ca7` (the walk) and `8e54255` (the
  gate hardening); `ad58bcc`, `4e629b5`, `b29ecdb` are the recorded rulings.
- **Against:** `VALUEPOSITIONSBUILDRULING` (and through it `VALUEPOSITIONS`,
  `REAUDIT79`).

## The build — one walk, driven from the grammar

`Disagreeing` no longer emits `ActionInValue`. The mismatch pass and the admissibility
pass had each grown a copy of the finding, and each new value position had to be found
twice — the treadmill. Now `Inadmissible` drives one walk off the grammar statements,
and two total classifiers decide what is a value position:

- **`Positions.ValuesOf(statement)`** — a statement's value expressions, each tagged
  whether the position CONSUMES the value (a datum initializer, a condition, an iterable,
  an association origin — its own action is a finding) or merely PERFORMS it (a bare
  expression statement, whose root is a standalone action run for effect and left legal,
  only its inner positions checked).
- **`Positions.PartsOf(value)`** — a grammar value's structural parts (a collection's
  entries, a group's inputs) and, if it is a reference, the tree to resolve.

Each value-position root resolves to a node, and **`Positions.Within(tree)`** walks the
value positions inside it — operands, call arguments, list and lookup entries — with the
round group **transparent**, so a bracketed action is reported once, at the action.

## The routes, all closed by the one walk

| route | witness | now |
|---|---|---|
| the four the checker already knew | typed init, typed call arg, written return, typed list elem | each an `ActionInValue`, now from the walk, not `Disagreeing` |
| `REAUDIT79` A — untyped peer | `var u = 5; var r = act 1 is u;` | caught — the walk consults no operator or peer sort |
| `REAUDIT79` B — no typer | `var r => number = act 1 otherwise n;` | caught — `otherwise`'s typing stays deferred, admissibility does not |
| `REAUDIT79` C — inferred aggregate | `[act x]` returned then unified | caught at the source `act x`, list and lookup twin |
| the constructs the set missed | `if/while/when act 1`, `for each y in act 1` | caught — the statement classifier's condition/iterable cases |
| an association among a group's inputs | `var q => number; var r = (q = act 1, 2);` | caught — `PartsOf` descends the input's association origin |
| a standalone action | `act 1;` | **left legal** — its root is performed, not consumed; `send (act 1);` still catches the argument |
| one action, once | `send ((act 1))` | **one** finding — grouping transparent, `Disagreeing` silent |

All in `Test/Integration/ValuePositions.cs`.

## The totality gate (§2, §3) — and where I stopped short of §4's letter

`_ => throw` in all three classifiers: an unclassified kind FAILS, it never returns the
empty set that would silently admit an action. The `none` kinds are explicit `=> []` arms
with a reason a reader reviews. On top of the throw, two reflection gates deliver §3 — a
construct fails the BUILD the moment it is added, not only when source reaches the throw:

- **statement classifier:** every concrete grammar statement kind is handed to `ValuesOf`
  on a field-less instance and must reach a case; the reviewed out-of-domain kinds (a
  recovery node, the two abstract bases, a collection element) must throw, so the exclusion
  cannot hide a kind left unclassified. Removing the `Association` arm fails it.
- **resolved-node classifier:** the concrete `Node` set is pinned to its roster (a resolved
  node dereferences its children, so it cannot be classified from a field-less instance).

**Where I want your eye — §4's second half.** §4 asked that *the gate assert the
membership of the `none` list*, so moving a kind into "no value positions" is a visible
diff. I built the completeness half (a new kind fails the build) but **not** a `none`-vs-
`has-positions` membership roster, and I want to be explicit that this is a judgment, not
an oversight:

- realising it by the same field-less reflection is **low-signal**: `PartsOf` and
  `Children` use property patterns (`{ Reference: { } }`) and dereference children, so an
  uninitialised instance throws or NPEs for reasons unrelated to classification; and
  `ValuesOf` on a field-less instance buckets never-occurring kinds (`Parameters`,
  `Algebra`) by whatever base arm they match, which would put noise in the very roster a
  reviewer is meant to read.
- what I have instead: the explicit commented `=> []` arms as the human roster, and the
  guarantee that a returned empty set **always** means "reviewed none" (never "forgot"),
  because "forgot" throws.

My read is that §3-completeness plus explicit arms buys most of what §4 wanted — a
silent-admitting construct can only be added by writing down `=> []` where a reviewer sees
it — but I did not build the structural `none`-membership assertion, and the honest,
low-signal reason is above. **Rule whether that suffices, or whether you want a corpus-based
`none` pin** (compile a witness per none-kind, assert no value-position finding) as a
follow-up.

## One case deleted as dead

The classifier as first sketched had an `IError => []` arm — recovery nodes stepped over.
It is unreachable: a module with any parse error is diagnosed and **never checked**
(findings-suppress-checking, `Declare`), so no recovery node reaches the walk. Confirmed by
probe (no malformed input covers it) and by the invariant. Deleted rather than covered with
a contrived input, per the delete-rather-than-defend discipline — the `_ => throw` now
stands where it would have, and the gate skips IError kinds as out-of-domain.

## Two adjacent gaps, named not absorbed (§5, §6)

Per §5, I checked whether a **not-a-target** rule exists. It does not:

```ronin
  act 1 = x     -- [Shadowed] — a wrong-reason finding (read as a declaration), not "not a target"
  5 = x         -- []          — a literal destination is silently accepted
```

The walk correctly touches only an association's **origin**, so it gives no wrong-reason
`ActionInValue` for `act 1 = x`. But a destination is a location that nothing checks: `5 =
x` is silently accepted, and `act 1 = x` reports the wrong thing. **This is a real gap — a
not-a-target admissibility rule — worth its own slice, not this walk's to absorb.**

Per §6, the bare discarded value is **silent today**:

```ronin
  5;            -- []   — a value computed and discarded, the mirror of the standalone action
  x;            -- []
```

Dead code by the same reasoning as `send return 5`, and not this ruling. **Raising it, as
you asked.**

## Gate at `8e54255` (net10.0)

- `restore --locked-mode` clean; Debug and Release build clean, `-warnaserror`.
- `Passed! — Failed: 0, Passed: 1372` (Release and Debug).
- Coverage **100%** line and **100%** branch.
- Changed-file `dotnet format --verify-no-changes`: clean. `git diff --check` clean.

Sabotage-verified by inverse edit, each failing a guarding test then restored: re-adding
`Disagreeing`'s action arm (double report), inverting the consumed-root flag (both
directions), dropping the `Call` arm of `Children` (arguments unwalked), removing group
transparency (double report), a construct arm returning `[]` (condition unseen), the
`PartsOf` throw made a silent default (totality gate), and the `Association` arm removed
(the statement completeness gate, at build time).

If a value position beyond those named can still carry an action to a ground sort, the
`Positions` classifiers are the one seam where another would be added — and the gate is
what makes adding one loud.
