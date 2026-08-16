# Re-audit 62 — Shared-signature publication and finding identity

**Audited:** `ac12a75..ede64ce`, principally `ede64ce`, against `REAUDIT61`,
`SCOPEIDENTITYRULING`, and `CONTAINERIDENTITYRULING`.

**Date:** 2026-08-16

## Result

**Not signed off. One medium-severity finding remains.**

`REAUDIT61` is closed. Every owner signature in a multi-body B container is now
resolved against the complete shared type table and published before any body
recurses. Both body orders produce the same correct pair of diagnostics, and the
all-same control produces one three-site duplicate.

The programmer's calibration note is correct for that witness. The inner and
module-level `Overloaded(2)` classifications have the same complete participant
set, so retaining only one of them is the intended outcome and the final count is
exactly two findings: one `DuplicateSignature` and one `Overloaded(2)`.

Reviewing that deduplication boundary found a separate defect. Finding identity
does not include related sites. Distinct invalid sets in sibling scopes can
therefore be collapsed when they share the same inherited primary declaration,
kind, and message.

No high-severity issue or unrelated regression was found.

---

## Finding

### 1. Medium — deduplication collapses distinct sibling-scope overload sets

`Compilation.Add` retains a finding only when `Identify` produces a new key
(`Compiler/Compilation.cs:1156-1173`). That key contains:

- finding kind;
- primary offset and length; and
- rendered message.

It omits every `Related` span. The surrounding comment says a finding involving
something added by an inner scope differs in its symbols or span and survives, but
that distinction is not represented in the key when the added declaration is a
related participant.

`Declarations.Classify` makes the first visible group or duplicate site primary
and attaches subsequent participants with `Alongside`
(`Compiler/Grammar/Declarations.cs:161-191`). Consequently, sibling scopes that
each combine the same inherited declaration with a different local declaration
can produce identical identity keys even though their participant sets differ.

The minimal overload witness is:

```ronin
function use (x => number) { return x; }

function left {
    function use (y => text) { return y; }
}

function right {
    function use (z => truth) { return z; }
}
```

There are two independently invalid visible sets:

- `left` sees outer `number` plus local `text`;
- `right` sees outer `number` plus local `truth`.

Both classifications are `Overloaded(2)`. Both choose the same outer `use` as
primary and render the same message. Only their related local site differs, so
`Add` drops the second finding. Current behavior reports one overload finding;
removing the first sibling causes the previously hidden second finding to appear.

The same loss occurs for permanent duplicates:

```ronin
function use (x => number) { return x; }

function left {
    function use (y => number) { return y; }
}

function right {
    function use (z => number) { return z; }
}
```

Each sibling introduces its own duplicate pair with the inherited declaration,
but only one `DuplicateSignature` survives. This is not merely duplicate traversal
of one conflict: the two findings identify different local declarations, and each
remains an error if the other sibling is deleted.

A mixed-count control confirms the exact mechanism. When one sibling has two
visible groups and the other has three, both findings survive because `Count` is
rendered into the `Overloaded` message. The loss occurs when distinct participant
sets happen to render the same kind, primary, and message.

Make finding identity include the complete participant identity, not just its
presentation anchor. For these set findings, a normalized sequence of the primary
and related source spans (and labels if labels can distinguish semantic roles) is
the natural key. This preserves the intended `REAUDIT61` deduplication because its
inner and module-level classifications contain the same participants, while the
two sibling sets above remain distinct. Simply disabling deduplication would
restore repeated reports from nested traversal and is not the required fix.

Maintain regressions for both sibling witnesses:

- two sibling `Overloaded(2)` sets with one shared inherited declaration must
  produce two findings, each naming its own local related site;
- two sibling permanent duplicate pairs must produce two `DuplicateSignature`
  findings; and
- the repaired `REAUDIT61` witness must continue to produce exactly two total
  findings, because its redundant overload classifications have the same complete
  participant set.

---

## Disposition of `REAUDIT61`

| Prior finding | Reassessment |
|---|---|
| shared overload signatures are published sequentially, after earlier bodies recurse | **Closed.** After the shared type table is built, all local owner signatures are resolved and written to both `shared.Overloads` and `declared.Overloads` before the first body recurses. The update is limited by owner source span, so inherited signatures retain the sorts resolved in their own containers. Both body-order controls now yield one duplicate plus one `Overloaded(2)`; the all-same control yields one duplicate with two related sites and no overload. |

## What the implementation gets right

- Publication now has the correct phase boundary: complete shared type collection,
  complete local-owner signature resolution, then recursive body processing.
- Both live tables are updated, so nested scopes and the later container-level
  classifier observe the same resolved signatures.
- Matching by each body's owner span restricts the eager publication to the local B
  set and does not re-resolve inherited candidates against the wrong type table.
- The later per-body resolution remains idempotent and does not change the answer.
- The new maintained test covers both body orders and the three-site all-same case.
- The intended redundant `Overloaded(2)` is deduplicated, giving exactly two total
  findings in the mixed duplicate/overload witness.

## Verification record

Temporary audit probes were removed from the worktree before this report was
written.

- Inspected the complete `ac12a75..ede64ce` production and test diff. No newer
  designer ruling was present in the handoff folder.
- Re-ran both `REAUDIT61` body-order witnesses: each reports exactly one
  `DuplicateSignature` and one `Overloaded(2)`.
- Re-ran the all-same witness: it reports one `DuplicateSignature` with two related
  sites and no overload.
- Probed two sibling scopes that each combine one shared inherited declaration with
  a distinct local type. Only one of the two `Overloaded(2)` findings survives.
- Probed the equivalent sibling duplicate case. Only one of the two independent
  `DuplicateSignature` findings survives.
- Probed sibling overload sets of different cardinalities. `Overloaded(2)` and
  `Overloaded(3)` both survive because their rendered messages differ.
- `dotnet build Ronin.sln --no-restore --configuration Release -warnaserror`:
  **passed**, 0 warnings and 0 errors.
- Release coverage gate from `.github/workflows/build.yml`:
  **1,285 passed, 0 failed, 0 skipped**; `Ronin` and `Ronin.Server` each report
  **100% line, 100% branch, and 100% method coverage**.
- `git diff --check` and `git show --check HEAD`: **clean**.

The only worktree addition made by this audit is this report. Existing untracked
handoff documents were left untouched; no production or maintained test file was
changed.
