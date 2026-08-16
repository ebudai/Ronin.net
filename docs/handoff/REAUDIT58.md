# Re-audit 58 — Shared overload containers and ruled value identities

**Audited:** `e00ee38..213a7ee`, principally `a64b251`, `35f8990`, and
`213a7ee`, against `REAUDIT57`, `VARIABLEANDMODULE`,
`CONTAINERIDENTITYRULING`, and `SCOPEIDENTITYRULING`.

**Date:** 2026-08-16

## Result

**Not signed off. One medium-severity finding remains.**

All three direct `REAUDIT57` findings are repaired. Requirements now own and
compare their operand tuples structurally; callers can supply a stable document
identity to a compilation; and the bodies of a registered overload set are built
over one shared type table, with their signatures resolved and classified once
against that table. The former valid-source witnesses now behave consistently in
body, parameter, return, nested-type, and ancillary-signature positions.

The remaining defect is adjacent to the overload fix. Bodies are joined by their
bare rendered container segment before the code establishes that their owning
functions are members of the same registered overload set. Consequently a function
whose declaration was rejected can donate body-local types to a valid same-named
function. This suppresses a real `UnknownType` and leaves the valid signature with a
semantic sort naming a declaration that is not in its overload container.

No high-severity finding remains, and no valid-source regression was found.

---

## Finding

### 1. Medium — a rejected function body participates in a valid overload container

`Compilation.Scope` first groups every named body solely by
`body.Container`, a string produced from `Identifier.Words`
(`Compiler/Compilation.cs:208-223`). If a group contains more than one body, it
collects the types from **all** of them into one shared declaration table
(`:244-252`). Only afterwards does it search for any registered function span in
the group and obtain one pattern to classify (`:256-269`).

That ordering makes registration irrelevant to container membership. A function
refused by `Declarations.Declare` is intentionally absent from `Overloads`
(`Compiler/Grammar/Declarations.cs:390-450`), but its body still enters the string
group and its types still enter the registered sibling's shared table.

The minimal audit witness is:

```ronin
function use () { type token; }
function use (x => token) { return x; }
```

The first declaration is rejected with `EmptyHole` and is registered nowhere. The
second declaration is valid, but `token` is not declared in its overload container.
Current behavior is nevertheless:

- the compilation reports only `EmptyHole`, with no `UnknownType` at the second
  signature;
- the registered `use (_)` signature's parameter sort is a `Sort.Named` for
  `token`, not null;
- removing the rejected first declaration makes the expected `UnknownType`
  reappear.

Thus adding an invalid, non-member declaration makes an unrelated error disappear
and changes the valid declaration's semantic signature. The source still fails to
compile, which limits the severity, but this is not merely an extra cascading
diagnostic: it is cross-declaration state contamination, and the partial semantic
model consumed by the editor and the coming checker is internally inconsistent.

The B ruling joins the bodies of **one overload set**. It does not join every
syntactic body whose identifier loses to the same word string, especially one the
declaration pass explicitly refused to put in that set.

Partition function bodies by the registered `Pattern`/owner relationship, using
the already-recorded signature span to establish membership before collecting
types. Share a table only among bodies registered under that exact pattern. Recurse
a refused function body independently so diagnostics inside it remain available,
but do not let it contribute declarations to a registered sibling. Type bodies and
same-word bodies belonging to a different pattern likewise should not enter that
overload table.

Maintain the witness above with both sides of the invariant:

- `EmptyHole` remains reported for the rejected declaration;
- `UnknownType` remains reported for the valid declaration and its stored parameter
  sort remains null.

A second control with two same-word but different/refused patterns would guard the
more general mistake: rendered container equality is not overload-set membership.

---

## Disposition of `REAUDIT57`

| Prior finding | Reassessment |
|---|---|
| 1. one B overload container had several visibility tables | **Closed for registered overload sets.** All bodies and ancillary transparent scopes contribute to one table; cross-body and cross-signature types resolve; equivalent spellings produce only `DuplicateSignature`; distinct groups still produce `Overloaded`; nested type members behave the same. Rejected same-word owners can currently enter that table, which is the new finding above. |
| 2. `Requirement` was neither structurally equal nor owned | **Closed.** Construction copies the operand sequence; equality and hashing include pattern, provenance, and each operand structurally. Independently built equals deduplicate, caller mutation cannot alter the stored tuple, and different operands remain distinct. |
| 3. pathless module identity was per compilation with no owner hook | **Closed.** `Compilation.Of` accepts a supplied `ModuleIdentity`; reusing a buffer handle preserves named-type identity across compilations, different handles remain distinct, and an explicitly supplied path is honored. With no owner, the one-shot pathless fallback still correctly mints a fresh buffer. |

## What the implementation gets right

- Overload classification no longer runs prematurely in each `Declarations.Of`.
  `Compilation.Scope` supplies the whole registered shape container and invokes
  `Classify` once.
- A type declared in one overload body is visible in another body's annotation and
  in another owning signature, including when the type comes from a parameter-default
  delegate.
- Parameter and return sorts stored on the root declarations are the shared
  container's semantic sorts, not stale spelling-only/null results.
- Equivalent body-local spellings such as `token` and `(token)` are a permanent
  duplicate, never the contradictory former combination of `Overloaded`,
  `DuplicateSignature`, and `UnknownType`.
- Cross-body type-name collisions are still reported once as `Shadowed`, with the
  later declaration primary and source order preserved.
- A repeated type body, which has no function pattern to classify, no longer causes
  the overload lookup to throw.
- `Requirement` now has the same owned structural-sequence discipline as the other
  semantic value objects.
- The module identity API puts stable buffer identity at the compilation boundary,
  where a document owner can actually supply it, without putting document lifetime
  onto `SourceText` snapshots.

## Verification record

Temporary audit probes were removed from the worktree before this report was
written.

- Inspected the complete `e00ee38..213a7ee` production and test diff and the current
  handoff rulings. No ruling newer than the documents governing `REAUDIT57` was
  present.
- Re-ran every original `REAUDIT57` witness and additional controls for:
  body/body visibility, body/signature visibility, ancillary/signature visibility,
  return annotations, equivalent spellings, three-member `A, A, B` classification,
  and a nested overload set inside a type.
- Reproduced finding 1 and its removal control against the real compilation entry
  point; the rejected body changes both findings and the stored root signature.
- `dotnet build Ronin.sln --no-restore --configuration Release -warnaserror`:
  **passed**, 0 warnings and 0 errors.
- Release coverage gate from `.github/workflows/build.yml`:
  **1,280 passed, 0 failed, 0 skipped**; `Ronin` and `Ronin.Server` each report
  **100% line, 100% branch, and 100% method coverage**.
- `git diff --check` and `git show --check HEAD`: **clean**.

The only worktree additions visible before and after the audit are the existing
untracked handoff documents plus this report; no production or maintained test file
was changed by the audit.
