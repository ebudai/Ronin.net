# Re-audit 51 — type-slice incorporation

**Audited:** `c797b5f..b4f9dfc`, the implementation and rulings responding to
`FRESHAUDIT21`. Finding 1 from that audit was deliberately excluded from this
incorporation and remains open.

**Date:** 2026-08-14

## Result

**No sign-off.** The ordinary grouped-type repair is sound, the keyword census
is repaired, and the designer's modifier-name ruling changes the disposition of
the former `fast` reservation finding. However, the implementation that admits
a keyed round type group introduces a compiler-terminating source path and
records the round group as a square runtime lookup. The new maintained test
checks only that the simple witness produces no finding, so neither defect is
guarded.

The exact changed-file formatting command prescribed by the prior handoff also
still exits 2. Its whitespace findings have been fixed; the remaining failures
are the pre-existing `IDE1006` diagnostics in a changed test file. The narrower
whitespace-only gate is green.

The full maintained suite otherwise remains healthy: locked restore, the
warning-as-error Release build, all 1,253 Debug and Release tests, exact 100%
line/branch/method coverage, and the direct plus transitive vulnerability audit
all pass.

---

## Findings

### 1. A keyed round type group is represented as a lookup and can crash ambiguity reporting

**Severity: high — source admitted by this resolver slice can terminate `Compilation.Of` with an
`ArgumentOutOfRangeException`, and the successful non-ambiguous path already
produces the wrong tree shape.**

The resolver now searches for an association in every type-mode group, which is
the right missing reachability step (`Compiler/Resolution/Resolver.cs:393-401`).
The implementation then works around `Node.Group`'s key invariant by changing a
keyed **round** group to `Node.Grouping.Lookup`
(`Compiler/Resolution/Resolver.cs:438-446`). That is not a neutral carrier:

- `Node.Grouping` expressly identifies the written delimiter and says `Group`
  is `(x)`, while `Lookup` is `[k = v]`
  (`Compiler/Resolution/Node.cs:220-240`);
- a `Lookup` renders with square brackets
  (`Compiler/Resolution/Node.cs:327-332`); and
- repair traversal deliberately treats lists and lookups as opaque collections
  (`Compiler/Resolution/Repair.cs:283-287`).

A direct resolver probe with declared `a` and `b` confirms the semantic change:

```text
source:   optional (a = b)
reading:  optional [«a» = «b»]
argument: Node.Group, Kind = Lookup
```

So the new test's claim that a keyed round group is “carried” is true only in
the weakest sense that resolution returns `Resolved`. The source delimiters and
node meaning are not preserved. A checker consuming this tree could not
distinguish the ruled type grouping from an actual lookup-shaped node by kind.

The same representation fails more severely when either side of the keyed
entry contains an arrow ambiguity. Each of these complete sources threw an
`ArgumentOutOfRangeException` instead of returning an `Ambiguous` finding:

```ronin
type a; type b; type c; type d;
var x => optional (a = b => c => d);
```

```ronin
var x => optional (number = text => number => truth);
```

```ronin
var x => optional (text => number => truth = number);
```

The control remains correct:

```ronin
var x => optional (text => number => truth);
```

It returns one `Ambiguous` finding with two readings and two repairs.

There are two joined causes in the failure path:

1. `Resolver.Group` offers the newly constructed group without assigning a
   source extent (`Compiler/Resolution/Resolver.cs:470-485`). The node therefore
   reaches `Repair.Search.Range` with the default zero-length extent. `Range`
   advances until a lexeme offset equals the node offset without an end guard
   (`Compiler/Resolution/Repair.cs:343-355`) and indexes beyond the lexeme list.
2. Adding an extent alone would not make the representation sound. A normal
   singleton round group is stripped to its contained operation during repair,
   but a lookup is opaque. The nested key/value ambiguity is part of one type
   annotation and is not repaired as separately compiled collection elements,
   so the current lookup traversal cannot expose the subtree whose bracketing
   selects a reading.

This is a root representation problem, not a request for a source-specific
branch. A keyed round type group needs an honest node representation that can
carry keys while retaining round grouping, valid source coordinates, and a
repair walk through its key and value type subtrees. It must not masquerade as a
runtime lookup merely because the existing `Node.Group` invariant allows keys
only on `Lookup`.

**Recommendation:** introduce the missing structural form (or otherwise extend
the node model without conflating delimiters), set its source extent at
construction, and define its identity, rendering, and repair traversal. Maintain
all of the following:

- a direct tree assertion for `optional (a = b)` covering node kind, round
  rendering, key, value, and source-contained non-empty extent;
- source tests with an ambiguous arrow in the key and in the value, each
  asserting two readings, two usable repairs, and no exception; and
- the existing value-mode refusal of `(a = b)`.

The current `ResolutionKind.Resolved` assertion
(`Test/Unit/TypeResolution.cs:78-93`) and empty-finding integration assertion
(`Test/Integration/TypeAnnotations.cs:129-145`) are insufficient because both
pass for the misrepresented tree and never enter repair generation.

### 2. The documented changed-file formatting gate still fails

**Severity: low process finding — whitespace is now clean, but the exact gate
the handoff says to run is not green.**

This command still exits 2:

```text
dotnet format Ronin.sln --verify-no-changes --include <changed files>
```

It now reports only the existing `IDE1006` naming warnings for the `Ambiguity`,
`Tie`, and `Probes` fields in `Test/Unit/Admission.cs`. The whitespace violations
reported by `FRESHAUDIT21` are gone, and this narrower command passes:

```text
dotnet format whitespace Ronin.sln --verify-no-changes --include <changed files>
```

The implementation therefore closes the whitespace defect but not the stated
gate. Either make the exact command green by resolving/suppressing the known
analyzer diagnostics, or change the documented gate explicitly to the
whitespace-only command if whitespace was its intended contract. A failing
general command must not be reported as passing because its remaining messages
predate this slice.

---

## Disposition of `FRESHAUDIT21`

| Prior finding | Reassessment |
|---|---|
| 1. No semantic checker | **Open by instruction.** This incorporation did not attempt it. |
| 2. `fast` absent from the name registry | **Reclassified as behaving as designed.** Under `MODIFIERNAMES.md`, the relevant datum probe has no valid modifier-before-name reading: `hidden type box => number` is a datum whose name includes those words, whereas `hidden type box;` is a type declaration. With no competing modifier reading, the conditional ruling reserves nothing. Adding `fast` to the registry would reinstate the rejected global reservation. |
| 3. `fast` validation | **Partly folded into `FRESHAUDIT21` finding 1, partly still a known future slice.** Target and duplicate validation are correctly recorded in the expiry ledger as checker work. General modifier placement remains explicitly deferred by `MODIFIERNAMES-RESULT.md`; probes confirm `fast if`, `fast while`, `fast when`, and `fast type` are still admitted. No completed placement matrix or contrary designer ruling exists, so this audit records that boundary rather than inventing one. |
| 4. Outermost grouped type rejected | **Closed.** `(number)`, a grouped function type, and a grouped function type parameter all traverse the normal source path correctly. The exception is limited to a round group in type capture (`Compiler/Grammar/Reference.cs:105-123`). |
| 5. Keyed type group rejected too early | **Not closed; regressed into finding 1 above.** The simple witness resolves, but the carrier has the wrong kind and nested ambiguous variants crash compilation. |
| 6. Changed-file formatter gate | **Partially closed; finding 2 above remains.** Whitespace is normalized, but the exact general formatter command still exits 2. |
| Keyword-boundary census omission | **Closed.** `fast` is included in the maintained keyword boundary table. |

The distinction in finding 3 matters for handoff accuracy. The designer has
ruled that modifier-name reservation is conditional and has assigned
target/duplicate checking to the checker. The designer has **not** declared
generic modifier placement complete or valid everywhere; the programmer's own
result document calls it a future language slice. It is therefore not a new
regression found here, but it also cannot be described as incorporated.

## What held up under reassessment

- The `Reference.Parse` change is type-mode-specific and restores lone round
  groups without admitting bare anonymous values as references.
- `(number)`, `(text => number)`, and a grouped function-type parameter compile
  cleanly and retain the expected source AST.
- `fast` has punctuation and EOF boundary coverage through the central keyword
  census.
- The conditional datum-head experiment required by `MODIFIERNAMES.md` has a
  clear result: the declaration production does not have a modifier-before-name
  slot, so there is no alternate modifier reading to protect by reserving every
  modifier word.
- `fast if`, `fast while`, `fast when`, and `fast type` still demonstrate the
  acknowledged generic placement question; they are not evidence for reviving
  the rejected name-reservation finding.
- The expiry ledger now records `fast` target and duplicate enforcement against
  the absent semantic checker rather than pretending the annotation resolver
  can validate them.

## Verification record

Temporary probe tests were removed before this report was written.

- Inspected the complete `c797b5f..b4f9dfc` production, test, and handoff diff,
  plus the adjacent node, repair, parser, modifier, symbol-table, compilation,
  and expiry paths.
- Read `FASTRESERVATION.md`, the designer's `MODIFIERNAMES.md`, and
  `MODIFIERNAMES-RESULT.md`, and applied the conditional ruling rather than the
  superseded recommendation in `FRESHAUDIT21` finding 2.
- Targeted type-annotation, type-resolution, boundary, and repair suite:
  **65 passed, 0 failed, 0 skipped**.
- `dotnet restore Ronin.sln --locked-mode`: passed.
- `dotnet build Ronin.sln --no-restore --configuration Release -warnaserror`:
  passed with zero warnings and zero errors.
- Full Debug suite: **1,253 passed, 0 failed, 0 skipped**.
- Exact Release coverage suite: **1,253 passed, 0 failed, 0 skipped**;
  **3,837/3,837 lines**, **2,641/2,641 branches**, and 100% methods.
- `dotnet list Ronin.sln package --vulnerable --include-transitive`: no known
  vulnerable direct or transitive packages in the configured source.
- Exact changed-file `dotnet format --verify-no-changes`: **failed**, finding 2.
- Changed-file `dotnet format whitespace --verify-no-changes`: passed.
- `git diff --check`: passed.
- `git diff -- Compiler Test`: empty after probe isolation. No production or
  maintained test file was changed by this audit.

The pre-existing untracked designer document `docs/handoff/MODIFIERNAMES.md`
was preserved. This report is the only artifact added by the audit.
