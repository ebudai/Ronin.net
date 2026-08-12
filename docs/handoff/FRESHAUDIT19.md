# Fresh audit 19 — repair coverage still identifies meanings by rendering

**Audited:** `4ba9f36..bd60ad9`, the three commits addressing
`FRESHAUDIT8` finding 6.

**Result:** no sign-off. The implementation substantially improves the old
circular property: declaration candidates now pass through production
`Compilation`, meaning counts are independently recomputed without the
resolver's chart, and generated production ambiguities are required to offer
distinct, working repairs. The named mutations are all detected.

One medium safeguard gap remains. The independent enumerator retains only a
count, while the repair property identifies the selected meaning by
`Node.ToString()`. That rendering is already known not to be injective for
nested calls. The tests can therefore accept two distinct edits that select the
same structural meaning while a different, identically rendered meaning has no
repair. The independent count remains correct, the number of repairs remains
correct, both edited files compile, and both rendering comparisons pass.

No production or maintained test file was changed during this re-audit. This
file is the only repository artifact added. Pre-existing documentation edits
and untracked handoff material were preserved.

---

## Disposition of `FRESHAUDIT8` finding 6

| required safeguard | re-audit result |
|---|---|
| Production declarations exercise generated admitted and forbidden candidates | **Closed.** `DeclarationAdmission` sends all 780 candidates through `Compilation`; disabling either `Rules.ReadsAs` or name `Infixes` makes the property fail with admitted names that do not read as themselves. |
| Meaning count is checked by an oracle independent of the production chart and rendering identity | **Closed for the generated grammar.** `MeaningCount` uses a separate recurrence over names, patterns, hole splits, and child trees. Reverting `Node.Same` to rendering identity makes it disagree with the resolver. |
| Every independently enumerated meaning has a selecting bracket repair | **Still open.** The enumerator returns only a cardinality. `RepairCoverage` checks production readings and repaired results by their non-injective renderings, not by independent structural identities; see finding 1. |

---

## 1. The repair property still uses a non-injective production rendering as its identity

**Severity: medium safeguard gap — the new property can pass when the repair
set has the right size but does not cover the structural meanings one to one.**

`Test/Unit/MeaningCount.cs:95-140` independently counts derivations, but its
oracle value is a `long`. It does not retain a structural identity for any tree:

```csharp
private static long Trees(...)
...
return Trees(words, 0, words.Length, []);
```

That count closes the most important half of the circularity. In particular,
changing `Node.Same` back to rendering equality makes the test fail on many
generated statements where the independent recurrence finds more meanings than
the resolver.

`Test/Unit/RepairCoverage.cs:91-114`, however, still checks repair identity this
way:

```csharp
Assert.Equal(finding.Readings.Count, finding.Repairs.Count);
Assert.Equal(finding.Repairs.Count, edited.Distinct(StringComparer.Ordinal).Count());
...
Assert.Empty(recompiled.Findings);
Assert.Equal(Grouped(repair.Reading), Grouped(Selected(recompiled)));
```

The first assertion compares two collections produced from the same production
resolution. The second proves that the source edits differ, not that their
selected trees differ. The last compares two renderings: `Repair.Reading` is
created from `alternative.ToString()` at
`Compiler/Resolution/Repair.cs:141-150`, and `Selected` returns the repaired
resolution's `Reading`, also derived from `Node.ToString()`.

That rendering cannot identify a meaning. `Compiler/Resolution/Node.cs:41-50`
records the concrete counterexample: two different nested call trees both
render as `print send «a» to «b»`. `MeaningCount` deliberately includes the
same family and notes that `print send a to b` has three structural trees while
two render alike.

Consequently, a repair regression can preserve every assertion above:

1. The independent oracle still counts `N` meanings.
2. Production still displays and publishes `N` entries.
3. Two different bracket strings uniquely select the same tree; redundant
   grouping is enough for distinct text not to imply distinct meaning.
4. Another tree with the same rendering receives no selecting repair.
5. Both edited files compile and both rendering comparisons succeed.

This is the identity form of the circularity finding 6 asked the new property
to remove. The maintained exact fixture for one two-tree collision is useful,
but it does not make the generated property structural or protect other parse
shapes.

**Recommendation:** make the repair assertion structural. The smallest change
is to recover the original `Resolution.Alternatives` for each finding, zip them
with the repairs in the order `Repairs.For` preserves, obtain the repaired
resolved `Node`, remove only repair-added singleton groups, and compare with
`Node.Same`. Also assert that the repaired structural trees are distinct.

The stronger version is to extend the independent enumerator from `long` counts
to canonical structural tree identities. Then assert that each original and
repaired production tree maps to the corresponding independent identity, and
that the repaired identities form a bijection with the independently
enumerated set wherever the full set is below the display cap. For bounded
cases, every displayed repair should still select a distinct identity belonging
to the independent set.

Do not use rendering as the identity in that assertion. It is useful as an
additional presentation check, but the known nested-call collision makes it
incapable of proving structural coverage.

---

## What the implementation now guards successfully

### Production declaration admission

`Test/Unit/DeclarationAdmission.cs` generates every one- through four-word
sequence over `a`, `b`, `send`, `is`, and `to`, sends all 780 candidates through
production `Compilation`, and asks the resolver whether each admitted name's
own span reads only as that name.

Mutation checks during this audit:

- replacing `Rules.ReadsAs` with `false` failed on admitted call-shaped names,
  beginning with `send a` and `send b`;
- disabling name `Infixes` failed on admitted comparisons, beginning with
  `a is a`, `a is b`, `b is a`, and `b is b`.

This closes the declaration-rule half of the prior finding for the generated
space, including forbidden candidates rather than manually omitting them.

### Independent meaning count

`Test/Unit/MeaningCount.cs` implements a separate memoised recurrence over
names, pattern segments, hole split points, and recursively counted children.
It shares the language vocabulary by transcription, but it does not call the
resolver's chart, cost logic, alternatives, renderings, or structural comparer.

Changing `Node.Same` to compare and hash `Node.ToString()` made the property
fail with many concrete disagreements, including an independently enumerated
count of two where the resolver returned one. This is the silent-collapse
mutation the old property could not see.

The maintained seeded sample covers 1,000 trials, including exactly 329 exact
ambiguities and 29 bounded totals. A temporary exhaustive audit probe checked
all 1,120 expressions in the generator's finite space; every exact count and
bounded floor agreed. The probe was removed with its detached audit worktree.

### Generated production repair coverage

`Test/Unit/RepairCoverage.cs` runs 1,000 seeded expressions through production
`Compilation`, encounters exactly 372 ambiguity findings, requires one distinct
edit per displayed reading, recompiles every edit, and requires the result to
be finding-free.

Reverting only the synthetic bracket coordinates made this property fail with
five displayed readings and four repairs, independently reproducing the
`FRESHAUDIT18` class.

A temporary exhaustive audit probe checked all 7,260 expressions in this
generator's finite space. Every ambiguity had the expected number of distinct
repairs, every edit compiled without findings, and every maintained rendering
comparison passed. This is strong evidence for the current implementation; it
does not remove finding 1 because the exhaustive probe intentionally exercised
the same non-structural identity assertion as the maintained property.

---

## Verification record

- `git diff --check 4ba9f36..bd60ad9` — passed.
- `dotnet restore Ronin.sln --locked-mode` — passed.
- `dotnet build Ronin.sln --no-restore --configuration Release -warnaserror` —
  passed with zero warnings and zero errors.
- Focused declaration, meaning-count, repair-coverage, and legacy completeness
  tests — 5 passed.
- Exact maintained Release coverage command — 1,198 passed; 100% line, branch,
  and method coverage for `Ronin` and `Ronin.Server`.
- `dotnet test Ronin.sln --no-restore --configuration Debug` — 1,198 passed.
- `dotnet list Ronin.sln package --vulnerable --include-transitive` — no known
  vulnerable direct or transitive packages in the configured source.
- Rendering-identity mutation — `MeaningCount` failed with independent count
  disagreements.
- `Rules.ReadsAs` disabled — `DeclarationAdmission` failed with call-shaped
  names admitted but not readable as themselves.
- name `Infixes` disabled — `DeclarationAdmission` failed with comparison-shaped
  names admitted but not readable as themselves.
- synthetic bracket coordinates removed — `RepairCoverage` failed with five
  readings and four repairs.
- Temporary exhaustive meaning probe — all 1,120 generator expressions passed.
- Temporary exhaustive repair probe — all 7,260 generator expressions passed.

The deliberately open `FRESHAUDIT8` finding 7 remains outside this audit and is
not counted again.
