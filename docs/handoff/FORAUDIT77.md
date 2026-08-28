# For audit — Slice 1(a): arithmetic and «is» type-checking, and the net10 move

> **Ledger** — `[R]` Requests audit of `427dabb..a8e3c79` (the `type-checker-and-editor-client` branch), against `SLICE-ONE-TYPINGS`. The first checker slice: operators gain a typer (result sort or null), «+ - * /» type as number arithmetic and «is» as require-unify equality with the ruled «optional T is T» carve-out; an operation's result flows outward, and a new pass reports operand disagreements. Foundational infrastructure — the typer mechanism, the «Operations» pass, and a group-unwrap in «Inferred» that broadened value inference globally — worth validating before «@» and the later slices build on it. Also carries the ruled net8→net10 move.
> supersedes: none
> superseded by: none

**From:** the successor, at `a8e3c79`. The branch is six commits: four are the
`SLICE-ONE-TYPINGS` / relayed-docs exchange (docs and ledger only, `--check` clean),
and two are code — the net10 upgrade and this slice. This is the first slice that
type-checks a construct the checker did not read before, so the foundation matters
more than the surface.

## For audit

- **Range:** `427dabb..a8e3c79` (whole branch). The **code** is two commits: `5b2041d`
  (net10) and `a8e3c79` (Slice 1a); the other four are the doc/ledger exchange.
- **Against:** `SLICE-ONE-TYPINGS` (§1 arithmetic, §2 «is» + the carve-out).
- **Not in scope:** «@» indexing (`SLICE-ONE-TYPINGS` §3) is Slice 1(b), next.
  «otherwise» typing (§4) is a later slice and deliberately carries no typer.

## What changed, and where to look

| piece | file | what to scrutinise |
|---|---|---|
| the typer | `Resolver.cs` `Operator.Typer` | a `Func<Sort,Sort,Sort>` init-property beside `Apply`/`BindingPower`; null where an operator has no value type |
| the typers | `Values.cs` `Numeric`, `Equality` | seeded from the one `Builtin.Operators` definition (single source). `error`, the bottom, unifies as either operand — a failure flowing into «+» is not a second finding |
| the «is» carve-out | `Values.cs` `Equality` | unify, else unwrap **one** «optional» either side. Trace the mutation order — does a failed first `Unify` corrupt the fallback? (I argue no: `Unify` binds only at the Variable top-level checks, and its only structural recursion is single-leaf List/Optional, so a failed unify never partially binds) |
| result flow | `Compilation.cs` `Operated` + the `Inferred` `Node.Operation` case | an operation's result feeds argument and return-type checks |
| **the group-unwrap** | `Compilation.cs` `Inferred` single-hole-`Group` case | **the blast-radius item.** This makes `Inferred` read `(v)` as `v` for EVERY value, not just operations — mirroring `Sort.Of`. It closed a real gap (return-of-bracketed-operation) and moved one existing test from asserting the gap to asserting the fix (`[a + b]` now `list of number`). Please check it surfaced nothing it should not |
| the pass | `Compilation.cs` `Operations` | mirrors `Arguments`; wired after it in the check loop. Skips an operator with no typer and an operand with no sort |
| the finding | `Finding.cs` `OperandType` | one kind, message switched on the operator symbol — the operator's own words say what it takes, so an unspellable operand needs no naming |

## Two things I want an opinion on

1. **The `OperandType` single-kind choice.** I used one finding kind whose message
   switches on the symbol («+» takes two numbers; «is» names «is a»). `SLICE-ONE-TYPINGS`
   §3 leaned toward a **distinct** kind for «@»'s not-indexable case. Is one kind with a
   per-symbol message right for «+»/«is», with «@» getting its own `NotIndexable` in 1(b)
   — or should operand-type findings be split per operator now, before the pattern sets?

2. **The initializer-bracket gap (flagged, not fixed).** A bracketed value in an
   INITIALIZER position — `var r => truth = (a + b)` — is **not** unwrapped, so it does not
   flag, while the same value in a **return** or an **argument** does. This is a
   pre-existing difference in how the initializer check reads its value (a path other than
   `Inferred`), which operations made newly visible. Left for its own slice — but if the
   auditor thinks it belongs here, say so.

## A parser note, for context (not a finding against this slice)

`1 is 2` between two bare LITERALS does not parse (`Malformed`), and `1 + 2` does — an
asymmetry in the resolver, unchanged by this slice (the typers are validated with
declared-name operands, `a is b`, which is the realistic form and how the tests read).
`return x is y` also binds as `(return x) is y` (the return-argument precedence). Neither
is this slice's to fix; noting them so a reproduction against literals is not mistaken for
a checker defect.

## Gate at `a8e3c79` (net10.0)

- `restore --locked-mode` clean; Debug and Release build clean, `-warnaserror`.
- `Passed! — Failed: 0, Passed: 1359` (Release).
- Coverage **100%** line and **100%** branch (`/p:Threshold=100 /p:ThresholdType=line,branch`).
- Changed-file `dotnet format Ronin.sln --verify-no-changes --include <files>`: clean.
- `git diff --check` clean. Ledger `--check` clean, both worklists at zero.

Sabotage-verified: neutering the arithmetic typer, dropping the «is» carve-out, unwiring
the «Operations» pass, and removing the group-unwrap each failed a guarding test, then
restored. (One process note for the record: a mid-slice `git checkout` to restore a
sabotage on uncommitted work reverted the slice in two files; it was fully re-applied and
the final commit is clean and gated as above.)
