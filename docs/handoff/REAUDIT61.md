# Re-audit 61 — Owner publication and shared-body sequencing

**Audited:** `513f14b..ac12a75`, principally `ac12a75`, against `REAUDIT60`,
`REAUDIT59`, `SCOPEIDENTITYRULING`, and `CONTAINERIDENTITYRULING`.

**Date:** 2026-08-16

## Result

**Not signed off. One medium-severity finding remains.**

The direct `REAUDIT60` defect is repaired. Once an owning function's signature is
resolved against its completed body table, the same result is now published to both
the enclosing registration and the body's inherited copy. The two-member direct and
parenthesized witnesses produce `DuplicateSignature`; the module-level control does
too; and the three-member case is one duplicate with both other sites related and no
temporary overload.

The same invariant is not yet established across several bodies of one B overload
container. Their common type table is complete before recursion, but their signatures
are still published one body at a time. A nested declaration in the first body can
therefore inspect a later sibling signature before that sibling has been resolved
against the shared table. Moving the same nested declaration into the later body
changes the diagnostic answer.

No high-severity issue or unrelated regression was found.

---

## Finding

### 1. Medium — shared overload signatures are published sequentially, after earlier bodies recurse

For several local bodies of one registered pattern, `Compilation.Scope` correctly
collects all body and ancillary types and creates one shared declaration table
(`Compiler/Compilation.cs:265-278`). It then calls the complete recursive `Scope`
for each body in order (`:280-282`).

Each call resolves only its own owner signature and publishes it to the shared and
current body tables (`Compiler/Compilation.cs:183-202`). But that same call also
recurses through every nested declaration before returning. Therefore the first
body's nested scopes run while later sibling signatures still hold their pre-body
null slots. The loop does not publish the next sibling until all work in the first
body is finished.

The minimal discriminating witness is:

```ronin
function use (x => number) {
    function use (z => token) { return z; }
    return x;
}

function use (y => token) {
    type token;
    return y;
}
```

The two outer bodies are one B container, so the `token` declared in the second is
visible throughout both. In the first body, the nested `use (_)` and the second
outer `use (_)` therefore both have `Sort.Named(module/use, token)`. The visible set
has two semantic groups:

- `number`; and
- `token`, with two declarations.

The correct diagnostics are one `DuplicateSignature` joining the two `token`
declarations and one `Overloaded` finding of count two between the number and token
groups.

Current behavior instead reports:

- no `DuplicateSignature`;
- an `Overloaded` finding of count three while the first body is being processed;
  and
- a second `Overloaded` finding of count two after the later sibling is finally
  resolved.

The later outer signature contributes fallback spelling `"token"` during the first
classification, while the nested signature contributes the actual `Sort.Named`, so
one semantic group is split into two.

The order control proves the sequencing dependency. Put `type token` in the first
body and the nested `use` in the second:

```ronin
function use (x => number) {
    type token;
    return x;
}

function use (y => token) {
    function use (z => token) { return z; }
    return y;
}
```

Now the first sibling has returned before the nested declaration is reached, the
second owner is published immediately before its own recursion, and the compiler
produces the correct `DuplicateSignature` plus `Overloaded(2)`. Moving a nested
declaration between bodies of the same shared container must not change the
candidate types or classification.

The all-same control exposes the same fault without a third sort:

```ronin
function use (x => token) {
    type token;
    function use (z => token) { return z; }
    return x;
}

function use (y => (token)) { return y; }
```

All three declarations have one sort and should form one three-site duplicate.
Current behavior splits off the unresolved later sibling, reporting a two-site
duplicate plus an erroneous overload.

Establish the shared-container signature table before entering any body recursively.
Once `shared` contains the complete B type environment, identify every local owner
signature by the body spans already available, resolve all of them against that
shared type table, and publish the results to both `shared` and the declaring table.
Only then recurse into the bodies. The per-body publication can remain as an
idempotent consistency step, but deeper scopes must never observe a partially
published sibling set.

This must remain targeted to the local B set. Inherited signatures from other named
containers retain their already-resolved owner sorts; broad re-resolution would
reopen `REAUDIT59`.

Maintain both body-order variants and assert identical semantic results:
`DuplicateSignature` for the token pair, one `Overloaded` of count two, and no
count-three overload. Maintain the all-same case with one duplicate and two related
sites, and no overload.

---

## Disposition of `REAUDIT60`

| Prior finding | Reassessment |
|---|---|
| late-resolved owner signature is absent from its own body table | **Closed for the current owner.** The signature resolved against the completed body table is written to both `enclosing.Overloads` and `declared.Overloads` by source span. Direct nested declarations now inherit the correct owning sort. Across multiple B siblings, later owners have not yet executed this publication when an earlier body recurses; that is the remaining finding above. |

## What the implementation gets right

- The owner signature is resolved once from the completed current table and
  synchronized into both live copies used by the parent and deeper body scopes.
- Structurally refused functions remain harmless: no matching registration means
  both publication searches simply miss them.
- Parameter and return sorts are synchronized together because the complete
  `Signature` record is replaced, not individual slots.
- The direct body-local named type and its parenthesized spelling are correctly one
  permanent duplicate when a nested same-pattern declaration inherits them.
- The three-direct-declaration test verifies related-site cardinality, preventing a
  superficially correct diagnostic kind over the wrong group.
- The `REAUDIT58` and `REAUDIT59` isolation, visible-count, and distinct-container
  behaviors remain intact in targeted controls.

## Verification record

Temporary audit probes were removed from the worktree before this report was
written.

- Inspected the complete `513f14b..ac12a75` production and test diff. No newer
  designer ruling was present in the handoff folder.
- Re-ran the exact `REAUDIT60` direct, parenthesized, module-level, and three-member
  witnesses; all now produce the maintained permanent-duplicate answers.
- Probed a nested matching declaration in the first and second bodies of the same B
  overload set. The first-body form produced `Overloaded(3)` plus `Overloaded(2)`
  and no duplicate; the second-body form produced `DuplicateSignature` plus
  `Overloaded(2)`.
- Probed three same-sort declarations with the nested declaration in the first body;
  it produced a two-site duplicate plus an erroneous overload instead of one
  three-site duplicate.
- `dotnet build Ronin.sln --no-restore --configuration Release -warnaserror`:
  **passed**, 0 warnings and 0 errors.
- Release coverage gate from `.github/workflows/build.yml`:
  **1,284 passed, 0 failed, 0 skipped**; `Ronin` and `Ronin.Server` each report
  **100% line, 100% branch, and 100% method coverage**.
- `git diff --check` and `git show --check HEAD`: **clean**.

The only worktree additions visible before and after the audit are the existing
untracked handoff documents plus this report; no production or maintained test file
was changed by the audit.
