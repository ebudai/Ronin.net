# Re-audit 60 — Visible classification and late owner-signature propagation

**Audited:** `e9579aa..513f14b`, principally `513f14b`, against `REAUDIT59`,
`REAUDIT57`, `SCOPEIDENTITYRULING`, and `CONTAINERIDENTITYRULING`.

**Date:** 2026-08-16

## Result

**Not signed off. One medium-severity finding remains.**

Both direct `REAUDIT59` findings are repaired. A local declaration now triggers
classification over the complete visible candidate set even when it is the only
local body. Inherited signatures whose owning sorts are already known are copied
without being rebound to the entered scope. The maintained number/number,
number/text, three-distinct-groups, and inner-duplicate controls all produce the
right classifications.

One late-binding path remains. A function signature may name a type declared in its
own body under H-wide. Its first resolution, before that body table exists, leaves a
non-null `ParameterSorts` structure containing null slots. Once the body table is
built the signature is correctly re-resolved, but the result is written only to the
enclosing declarations, not to the body declarations that deeper scopes inherit.
The new “preserve every non-null signature” rule then faithfully preserves that
stale copy. A nested same-pattern declaration consequently compares a fallback
spelling with the actual owning sort and is classified as a temporary overload
instead of a permanent duplicate.

No high-severity issue or unrelated regression was found.

---

## Finding

### 1. Medium — a late-resolved owner signature is not propagated into its own body table

`Compilation.Scope` builds the current body's `declared` table before resolving the
owning function's signature against it (`Compiler/Compilation.cs:145-181`). The
owner signature was copied from `enclosing`; if it names a body-local type, its
parameter structure exists but the relevant sort slot is null.

Once the body's types are available, lines `183-199` resolve the owner correctly.
They update only the matching signature in `enclosing.Overloads`. The separate copy
in `declared.Overloads` remains stale. Recursion into nested bodies later uses that
current `declared` table as its enclosing environment (`:203-292`).

The new preservation rule in `Declarations.Of` correctly avoids reinterpreting
known inherited signatures, but its marker is whether `ParameterSorts` itself is
null (`Compiler/Grammar/Declarations.cs:119-130`). A stale signature whose block
structure is non-null and whose slot is null therefore looks “already resolved” and
is preserved into every deeper table.

The minimal witness is:

```ronin
function use (x => token) {
    type token;

    function use (y => token) {
        return y;
    }

    return x;
}
```

Both signatures name the same `Sort.Named(module/use, token)`: the nested function
inherits the outer function's `token`, and shadowing prevents it from declaring a
different one under the same spelling. No argument type can choose between the two
`use (_)` declarations, so the result must be the permanent
`DuplicateSignature`.

Current behavior is one `Overloaded` finding of count two and no
`DuplicateSignature`. The outer candidate contributes the fallback string
`"token"` because its copied slot is still null; the nested candidate contributes
the actual `Sort.Named`. `Classify` therefore sees two groups even though both
declarations have the same semantic parameter type. Writing the nested spelling as
`(token)` produces the same wrong result.

The control isolates the late update: moving `type token;` to module scope makes the
sort available during the first resolution, and the same nested pair is correctly
reported as `DuplicateSignature`.

With two nested declarations, the distortion is larger:

```ronin
function use (x => token) {
    type token;
    function use (y => token)   { return y; }
    function use (z => (token)) { return z; }
    return x;
}
```

All three signatures have one semantic sort and should form one three-site duplicate
with no overload. Current behavior reports a duplicate only for the two nested
declarations plus an overload between that group and the stale owner.

This also affects the coming checker independently of classification: recursive
calls and deeper declarations inside a function body read the body's table, so they
would see the owner's pre-body signature rather than the semantic signature stored
at its actual declaration.

Resolve the owner once against the completed body table, then publish that same
resolved `Signature` to every live table copy representing its source span: the
enclosing registration and the current body's inherited copy. Apply the invariant
to singleton and shared B-container bodies; each body's current table must see the
same owner signature that is written back to its enclosing/shared table before
recursing into deeper bodies.

Do not restore broad inherited re-resolution. `REAUDIT59` correctly removed it: an
unrelated inherited signature must retain its original owner sort. This repair is
targeted propagation of the one signature just resolved at its owner.

Maintain the minimal witness, the parenthesized equivalent, the module-level-type
control, and the three-declaration case. Assert exact diagnostic kinds and related
sites—not merely that compilation fails—because the defect is precisely the
permanent/temporary classification boundary.

---

## Disposition of `REAUDIT59`

| Prior finding | Reassessment |
|---|---|
| 1. one local declaration of an inherited pattern bypasses classification | **Closed.** The singleton path no longer exits before classification. A local declaration is the trigger and `declared.Overloads[pattern].Count` supplies the visible set. Number/number produces `DuplicateSignature`; number/text produces `Overloaded(2)`. |
| 2. inherited signatures are rebound to the inner overload container | **Closed for completed owner signatures.** `Declarations.Of` preserves inherited signatures with populated parameter structures and resolves only newly declared records. Distinct outer and inner named types remain three groups, while two equivalent inner spellings form their own duplicate group. A signature corrected only after its body table is built is not synchronized into that body table; this is the remaining finding above. |

## What the implementation gets right

- Classification is now based on the visible candidate count rather than local body
  count, while purely inherited sets are not repeatedly classified in every child.
- Singleton and multi-body paths both resolve their local owners before the visible
  set is classified.
- Already completed inherited signatures retain their owning `Named` container and
  are not reinterpreted from source spelling in an unrelated inner table.
- Multi-body B containers still share their complete body and ancillary type table,
  enforce cross-body uniqueness, and write the locally resolved signatures back to
  the declaring table before classification.
- The exact `REAUDIT58` refused-owner isolation behavior remains intact.
- The two `REAUDIT59` maintained tests exercise permanent versus temporary
  classification, group counts, and the distinct named-container identity case.

## Verification record

Temporary audit probes were removed from the worktree before this report was
written.

- Inspected the complete `e9579aa..513f14b` production and test diff. No newer
  designer ruling was present in the handoff folder.
- Re-ran both `REAUDIT59` witness families and controls: inherited singleton
  duplicate, inherited singleton distinct sorts, three distinct named/scalar groups,
  and an inner duplicate group beside a distinct inherited type.
- Probed late owner resolution with a body-local type, an equivalent parenthesized
  spelling, a module-level control, and three same-sort declarations. Both two-member
  body-local witnesses produced `Overloaded`; the three-member witness split into
  `DuplicateSignature` plus `Overloaded`, as finding 1 predicts.
- `dotnet build Ronin.sln --no-restore --configuration Release -warnaserror`:
  **passed**, 0 warnings and 0 errors.
- Release coverage gate from `.github/workflows/build.yml`:
  **1,283 passed, 0 failed, 0 skipped**; `Ronin` and `Ronin.Server` each report
  **100% line, 100% branch, and 100% method coverage**.
- `git diff --check` and `git show --check HEAD`: **clean**.

The only worktree additions visible before and after the audit are the existing
untracked handoff documents plus this report; no production or maintained test file
was changed by the audit.
