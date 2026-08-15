# Re-audit 52 — keyed type-group incorporation

**Audited:** `b4f9dfc..7f881dd`, principally `083c86b` and `1bd252c`, against
`REAUDIT51`, the current type-half rulings, and the ordinary compilation path.

**Date:** 2026-08-15

## Result

**No sign-off. Both `REAUDIT51` findings are closed, but one already-ruled type
ambiguity is not repair-complete.**

The keyed round-group implementation is now structurally sound on the reported
path. A keyed round group has its own kind rather than masquerading as a square
lookup; groups carry their written extents; repair traversal reaches keyed type
subtrees; and the former nested-arrow sources return repairable ambiguity
findings instead of terminating compilation. Generated probes also held for
nested keyed groups, multiple entries, independently ambiguous key/value pairs,
and source-contained extents.

The exact changed-file `dotnet format --verify-no-changes` command is green, so
the second prior finding is closed too.

During adversarial repair verification, however, the expressly ruled type

```ronin
var table => lookup text => number => truth;
```

reported three readings and only two repairs. The missing selectable reading is
the function taking a lookup:

```ronin
var table => (lookup text => number) => truth;
```

All three designed bracketings resolve uniquely when written by hand. This is a
repair-search omission, not an unreachable meaning. The maintained test says
the finding offers all three but asserts only the reading count, so the defect
passes the suite.

The full maintained health remains excellent: locked restore, the
warning-as-error Release build, all 1,254 Debug and Release tests, exact 100%
line/branch/method coverage, the exact formatter gate, and the direct plus
transitive vulnerability audit all pass.

---

## Findings

### 1. Constructor/arrow ambiguities omit operation-shaped repairs

**Severity: high — the language's named three-way type ambiguity violates its
explicit repair-completeness ruling and presents a meaning the editor cannot
select.**

`LOOKUPARROWRULED.md` §2 and `ARROWASSOCIATIVITY.md` §1 require this source to
produce three readings with three selectable bracket repairs:

```ronin
var table => lookup text => number => truth;
```

Actual source-path result:

```text
readings: 3
repairs:  2
```

The two published repairs select:

```ronin
lookup text => (number => truth)
lookup (text => number) => truth
```

The omitted third reading is present in the diagnostic and is uniquely
reachable:

```ronin
(lookup text => number) => truth
```

Direct resolution confirmed that each of those three designed bracketings is
`Resolved`. There is therefore no budget, display-cap, grammar, or
expressibility reason to omit one.

The root is the structural boundary in `Repair.Search.Divergence`
(`Compiler/Resolution/Repair.cs:306-355`). It descends through an operation only
when **both** target and competitor are operations with the same symbol
(`Repair.cs:313-314`). When the target is the outer function-type `Operation`
and the competitor is a constructor `Call`, execution falls through to the
call-oriented branch (`Repair.cs:331-353`):

- at top level, it treats the entire operation as one candidate span and rejects
  it as the whole statement, so no divergence is returned;
- inside a keyed group, the same span is no longer the whole annotation, so it
  first brackets the entire still-ambiguous key or value. That bracket is idle;
  the next pass can find no deeper divergence and drops the repair.

This explains both the ruled top-level reproduction and the broader keyed-group
matrix. A temporary 37-case type-constructor matrix found 30 cases where at
least one displayed alternative received no repair. Representative results:

| annotation | meanings shown | repairs |
|---|---:|---:|
| `lookup text => number => truth` | 3 | 2 |
| `optional (number = lookup text => number => truth)` | 3 | 2 |
| `optional (lookup text => number => truth = number)` | 3 | 2 |
| `optional (number = optional text => number => truth)` | 5 | 2 |
| `optional (number = list of text => number => truth)` | 5 | 2 |

The simple keyed-group cases added in this incorporation contain only the bare
two-reading arrow operation. They correctly produce two repairs, but never put
an `Operation` alternative against a constructor `Call`, so they do not exercise
this boundary (`Test/Integration/TypeAnnotations.cs:148-168`).

The older lookup test describes exactly the required contract—“there are three
bracketings and the finding offers all three”—then asserts only `Total == 3`
(`Test/Integration/TypeAnnotations.cs:181-185`). It therefore proves derivation
completeness while leaving repair completeness untested.

**Recommendation:** make divergence find a selectable target subtree when node
kinds differ, including `Operation` versus `Call`, without bracketing an idle
whole ambiguity. Maintain the ruled source with all of these assertions:

- `Total == 3` and `Repairs.Count == 3`;
- applying each repair produces a finding-free, uniquely resolved annotation;
- the three repaired trees are structurally distinct and match the three
  displayed alternatives; and
- one repair selects exactly the function-taking-a-lookup reading.

Then extend the generated repair property to the type grammar. The current
`RepairCoverage` generator uses value calls and names but no type constructors,
arrow operations, or keyed type groups, so its 372 ambiguities cannot expose
this class. A compact product over scalar types, `optional`, `list of`, `lookup`,
bare arrow chains, and keyed groups found the defect immediately and guards the
structural relationship rather than one spelling.

### 2. The new fourth grouping kind leaves the surrounding structural contract false

**Severity: low documentation defect — implementation behavior is correct, but
the central node invariant now describes a different model from the code.**

`Node.Grouping.Keyed` is the right representation. Several adjacent contracts
still state that there are three kinds and that only a lookup can have keys:

- the enum summary and rationale say “three” three times
  (`Compiler/Resolution/Node.cs:219-229`);
- `Entry.Key` says it is null for a group or list, without distinguishing the
  newly keyed round group (`Node.cs:258-274`);
- `Disagreeing` still explains a “kind of three” whose identity consults a key
  only for a lookup (`Node.cs:276-289`);
- `Group.Kind` asks which of three bracket forms was written
  (`Node.cs:324-335`); and
- `Group.Alike` says a lookup has keys and “the other two” do not, directly above
  code that correctly includes `Grouping.Keyed` (`Node.cs:352-367`).

These are not harmless historical notes: they document the invariant enforced
by the constructor and consumed by identity, hashing, traversal, rendering, and
future checking. A successor following the prose could remove the very
distinction this incorporation added.

**Recommendation:** rewrite the contract as four representations—round group,
list, lookup, and keyed round group—and state the actual key invariant once:
`Lookup` and `Keyed` require a key on every entry; `Group` and `List` forbid one.
Update the `Entry.Key`, `Disagreeing`, `Kind`, and `Alike` remarks to use that
same partition.

---

## Disposition of `REAUDIT51`

| Prior finding | Reassessment |
|---|---|
| 1. keyed round group represented as a lookup and crashes repair | **Closed on the reported class.** `Grouping.Keyed` preserves round syntax and keys; identity and hashing include keys; every constructed group has its written extent; repair stripping and divergence descend through keyed entries. The prior key/value crash witnesses now return two readings and two repairs. Nested groups, multiple entries, independently ambiguous entries, and extent containment also held. Finding 1 above is an older, broader operation-versus-call repair gap exposed by extending the probe beyond the requested two-reading cases; it is not a recurrence of the lookup-shaped node. |
| 2. exact changed-file formatter gate fails | **Closed.** The private fields in `Test/Unit/Admission.cs` now follow the configured camel-case rule, and the exact general formatter command exits 0 with no formatted files or diagnostics. |

The semantic checker from `FRESHAUDIT21` finding 1 remains deliberately open;
this incorporation did not claim to implement it. `fast` target/duplicate
validation and the general modifier-placement slice retain their previously
documented dispositions.

## What held up under reassessment

- `Grouping.Keyed` is distinct in rendering, structural equality, and hashing;
  it does not evaluate as a runtime lookup.
- Every group offered by `Resolver.Group`, including empty groups and
  collections, receives the extent of its written brackets.
- The keyed traversal reaches keys, values, later entries, and nested keyed
  groups. The former `ArgumentOutOfRangeException` did not recur.
- The three maintained keyed ambiguity sources each return two readings and two
  repairs, including an ambiguous second entry after an unambiguous first one.
- Direct extent probes over ordinary, empty, nested, multi-entry, and keyed type
  groups found every tree node positive-length and contained by its source.
- Value-mode `(a = b)` remains refused, while type-mode
  `optional (a = b)` resolves to a round `Keyed` node with distinct key and value.
- The analyzer rename is confined to private test fields and preserves the
  admission probes.

## Verification record

Temporary audit probes were removed before this report was written.

- Inspected the complete `b4f9dfc..7f881dd` production, test, and handoff diff,
  plus the adjacent resolution, repair, compilation, node-identity, and type
  ruling paths.
- Read the current `LOOKUPARROWRULED.md`, `ARROWASSOCIATIVITY.md`,
  `TYPECHECKERHANDOFF.md`, `TYPEHALFRULINGS.md`, and modifier-name disposition.
- Focused maintained resolution, annotation, repair, ambiguity, and admission
  suite: **191 passed, 0 failed, 0 skipped**.
- Direct keyed-group probes covered both ambiguous sides, two independent
  ambiguities, multiple entries, nested keyed groups, lookup/function
  competition, repair application, and node extent containment.
- `dotnet restore Ronin.sln --locked-mode`: passed.
- `dotnet build Ronin.sln --no-restore --configuration Release -warnaserror`:
  passed with zero warnings and zero errors.
- Full Debug suite: **1,254 passed, 0 failed, 0 skipped**.
- Exact Release coverage suite: **1,254 passed, 0 failed, 0 skipped**;
  **3,851/3,851 lines**, **2,661/2,661 branches**, and 100% methods.
- `dotnet list Ronin.sln package --vulnerable --include-transitive`: no known
  vulnerable direct or transitive packages in the configured source.
- Exact changed-file `dotnet format --verify-no-changes`: passed, formatted zero
  files, and emitted no diagnostics.
- `git diff --check`: passed.
- `git diff -- Compiler Test`: empty after probe isolation. No production or
  maintained test file was changed by this audit.

The working tree was clean before this report was added. This report is the only
audit artifact.
