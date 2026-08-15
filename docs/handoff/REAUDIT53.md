# Re-audit 53 — type-repair incorporation

**Audited:** `7f881dd..57f36e3`, principally `ddd7287` and `53803cc`, against
`REAUDIT52`, the type-arrow rulings, and the compilation/editor boundaries.

**Date:** 2026-08-15

## Result

**Sign-off on the `REAUDIT52` incorporation. No new finding.**

The ruled three-way type ambiguity now exposes all three readings with all
three selectable repairs:

```ronin
var table => lookup text => number => truth;
```

The formerly omitted function-taking-a-lookup repair is present:

```ronin
var table => (lookup text => number) => truth;
```

Each repair applies to a finding-free, uniquely resolved annotation, selects a
different structural tree, and reaches the editor as a distinct applicable code
action. The repair algorithm also handles the broader operation-versus-call and
misaligned-operation classes rather than recognizing this spelling specially.

The fourth grouping kind's surrounding documentation now states the same
partition the constructor, identity, hashing, and traversal enforce: `Lookup`
and `Keyed` key every entry; `Group` and `List` key none.

The maintained build health remains fully green: locked restore, the
warning-as-error Release build, all 1,255 Debug and Release tests, exact 100%
line/branch/method coverage, the exact changed-file formatter gate, and the
direct plus transitive vulnerability audit all pass.

This is a scoped sign-off on the findings being incorporated. The semantic
checker identified by `FRESHAUDIT21` finding 1 remains deliberately open, as do
the previously ledgered checker-dependent `fast` checks and the separate generic
modifier-placement slice. This incorporation does not claim to complete those
larger language stages.

---

## Disposition of `REAUDIT52`

| Prior finding | Reassessment |
|---|---|
| 1. constructor/arrow ambiguities omit operation-shaped repairs | **Closed.** The exact ruled source returns three readings and three repairs, including `(lookup text => number) => truth`. All three repairs apply cleanly and uniquely, produce three distinct edits/trees, and surface as three editor actions. The fix is structural: equal-span operations descend together, misaligned operations are bracketed at their own span, and a same-span operation against a call exposes its operands instead of adding an idle whole-span pair. |
| 2. fourth grouping kind leaves the surrounding contract false | **Closed.** The enum, entry-key contract, invariant exception, kind property, and structural-equality remarks all describe four representations and the correct two-keyed/two-unkeyed partition. |

## Adversarial reassessment

The new maintained property is materially stronger than the previous fixture.
`TypeRepairCoverage` generates scalar, `optional`, `list of`, `lookup`, arrow,
and keyed-group type expressions. Across its deterministic 2,000 trials it
encounters exactly 730 ambiguities and requires, for every displayed reading:

- one repair per alternative;
- a uniquely resolved result after applying the repair;
- structural equality with the intended alternative after removing repair
  grouping; and
- a distinct selected tree for every published repair.

That directly guards the relationship `REAUDIT52` found absent. It uses the same
resolver instance intentionally because operation identity includes the chosen
operator instance.

Temporary audit probes extended that property beyond its maintained depth-three,
single-entry generation:

- a deterministic product over scalar, bare-arrow, `lookup`, `optional`, and
  `list of` expressions;
- four-arrow combs and constructor/arrow competition;
- independently ambiguous keys and values;
- two-entry keyed groups;
- nested keyed groups on either side of an entry;
- a second seed of 500 depth-four generated expressions, with more than 100
  ambiguous cases; and
- three multi-entry/nested examples through full source compilation rather than
  direct resolver entry.

Every displayed alternative received a repair, every repair re-resolved
uniquely to the intended structural tree, and every source-level repair cleared
the ambiguity. No idle whole-span insertion, dropped right-nested comb, missing
operation-shaped target, exception, or repair-budget failure appeared.

The editor boundary was checked separately. `Language.Actions` returned three
distinct actions for the ruled lookup-arrow annotation; one applied exactly the
missing `(lookup text => number) => truth` form, and all three edited documents
reported no diagnostic.

## What the implementation gets right

- The new span-alignment guard prevents operand-by-operand comparison of
  operations that occupy different words. This closes the right-nested arrow
  failure the programmer found while implementing the audit, not only the
  reported operation-versus-call case.
- When an operation and call occupy the same span, the operation's operands are
  candidate repair subtrees. The whole operation is not bracketed idly.
- Competitor operation operands are not treated as call arguments, avoiding a
  false leaf alignment inside a larger operation.
- The prior keyed-group descent remains intact for keys, values, later entries,
  and nested keyed groups.
- The source integration test now asserts repair count, applies each edit, checks
  distinct results, and pins the formerly missing repair text. Its old comment no
  longer claims more than its assertion proves.
- The generated test compares trees rather than renderings, preserving the
  distinction between differently nested meanings that print alike.
- The node documentation now agrees with the executable invariant at every place
  named by `REAUDIT52`.

## Verification record

Temporary audit probes were removed before this report was written.

- Inspected the complete `7f881dd..57f36e3` production, test, and handoff diff,
  plus adjacent resolver-node identity, repair search, compilation diagnostics,
  editor actions, and arrow/grouping rulings.
- Focused maintained annotation, type-resolution, type/value repair property,
  ambiguity, admission, resolution, and editor suite:
  **214 passed, 0 failed, 0 skipped**.
- Maintained type repair property: **730 generated ambiguities**, all displayed
  alternatives uniquely and structurally repaired.
- Additional deterministic/depth-four/source/editor probes: passed; removed
  before handoff.
- `dotnet restore Ronin.sln --locked-mode`: passed.
- `dotnet build Ronin.sln --no-restore --configuration Release -warnaserror`:
  passed with zero warnings and zero errors.
- Full Debug suite: **1,255 passed, 0 failed, 0 skipped**.
- Exact Release coverage suite: **1,255 passed, 0 failed, 0 skipped**;
  **3,859/3,859 lines**, **2,671/2,671 branches**, and 100% methods.
- `dotnet list Ronin.sln package --vulnerable --include-transitive`: no known
  vulnerable direct or transitive packages in the configured source.
- Exact changed-file `dotnet format --verify-no-changes`: passed, formatted zero
  files, and emitted no diagnostics.
- `git diff --check`: passed.
- `git diff -- Compiler Test`: empty after probe isolation. No production or
  maintained test file was changed by this audit.

The working tree was clean before this report was added. This report is the only
audit artifact.
