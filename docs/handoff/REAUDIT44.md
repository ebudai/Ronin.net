# Re-audit 44 — R7 incorporation and its remaining boundaries

**Re-audited:** `4194b7b..24249e4`

**Date:** 2026-08-05

## Result

**All four REAUDIT43 production findings are resolved. One low-severity report
correctness issue and two low-severity safeguard/gate issues remain. No clean
sign-off yet, but no current source-language correctness blocker was found in
the incorporation.**

The three-declaration diagnostic now blames the declaration that actually
completed the conflict and attaches the other two. The structurally sound set
is computed before every downstream diagnostic relation. R7 now treats pins as
pattern identity, including shifted later-hole indices, and the lookup encoder
reads each allocating canonical token spelling once.

I also exercised the ordering case requested but not added to the maintained
matrix: a local shorter pattern beneath inherited longer-pattern and name
declarations. It correctly blames the local shorter pattern and attaches both
inherited declarations.

The remaining production-adjacent issue is that the generated reservation
registry does not consume the same sound pattern set as diagnostics. It can
therefore publish a reservation from a pattern the compiler says cannot exist.
Two test issues are separate: the ordering-test rewrite accidentally deleted
the empirical cost and first-hole guards, and the wall-clock scaling test
failed once under the ordinary parallel suite before passing in isolation and
on rerun.

## Disposition of REAUDIT43

| Prior finding | Re-evaluation |
|---|---|
| 1. R7 orders only two of three declarations | **Resolved.** All six same-scope orders pass. The finding has three explicit repair cases and two related spans. A temporary nested-scope source probe also confirmed that a local shorter pattern wins ordering over inherited name/longer parties. |
| 2. Structurally invalid patterns reserve names | **Resolved in diagnostics.** `sound` is computed first; structural findings see all patterns and every relational diagnostic sees only `sound`. Finding 1 below covers the registry's remaining bypass of that boundary. |
| 3. R7 treats pinned holes as free | **Resolved.** The refined shorter hole must be free, every other pin is compared at its corresponding pre/post-insertion index, and the longer pattern may independently pin the surviving refined hole. The focused constructor rows pass. |
| 4. Lookup identity canonicalises twice | **Resolved.** `Identity` caches `token.Canonical` once and uses the local for both the length prefix and content. |

## Findings

### 1. The registry still publishes relationships from structurally forbidden patterns

**Severity: low — a generated language-safety report can claim a breaking
reservation which validation itself excludes; the current built-in table is
sound, so shipped output is not presently wrong**

`Rules.Validate` now establishes the right invariant at
`Compiler/Diagnostics/Rules.cs:105-119`: a pattern wrong in itself reports its
structural finding and does not participate in `Anchors`, `Shadowing`,
`Refining`, or `Glue`.

That soundness predicate is private to `Rules`, however. `Glue.Registry`
rebuilds its table directly from every supplied pattern
(`Compiler/Diagnostics/Glue.cs:89-91`) and uses the unfiltered table for glue,
free/anchor classifications, and the shared refinement relation
(`Glue.cs:123-164`). The shared relation is therefore semantically shared but
its eligibility rule is not.

A focused registry probe supplied:

```csharp
Glue.Registry([
    Pattern.Parse("send _ to _"),
    Pattern.Parse("send _ to otherwise _"),
]);
```

The compiler now correctly emits only `InfixInPattern` for that pair plus a
matching name. The registry nevertheless prints:

```text
otherwise    send (_) to otherwise (_) is send (_) to (_) with it at a hole
```

under `RESERVES A NAME PREFIX BY REFINING`. Its header says these are patterns
in scope and that adding a line is a breaking change. The second pattern cannot
legally enter the language, so `otherwise` is not reserved by that relation.
The same boundary affects the other registry sections: an `old` pattern may be
listed as an anchor prefix, and a pattern rejected for injected glue may still
be listed as reserving that glue.

**Recommendation:** make structural soundness one shared predicate and apply it
once before every registry classification, or make `Registry` explicitly
reject an unsound input table instead of emitting a report about it. Add the
operator-word refinement above as a cross-path test: validation should produce
one structural finding, and the registry must not claim its prefix reservation.

### 2. Expanding the ordering matrix deleted the R7 premise and first-hole safeguards

**Severity: low — production is correct, but the suite accepts recurrence of a
previous audited defect and no longer proves the diagnostic's stronger cost
claim**

The rewrite of `Test/Integration/NameShadowing.cs` appropriately replaced the
old two-party ordering/repair tests with all six three-party permutations. Its
edited range also deleted two tests which were not ordering tests:

- `AndTheReadingItTakesCanBeCheaperRatherThanMerelyEqual`, the resolver witness
  showing `send x to all count of items` remains resolved but changes from the
  intended call to the strictly cheaper absorbing name; and
- `AndInsertingAtTheFirstHoleIsR6sNotThis`, the source regression that includes
  a matching `all ...` name and requires exactly one `AnchorPrefix` finding.

The first matters because the current maintained test only asserts that the
message *says* “sometimes for less”; the `all count of items` inline row asserts
R7's blanket declaration finding, not resolver cost. A future resolver change
could invalidate the stated premise while every current assertion continued to
pass.

The second is demonstrably unguarded. I temporarily changed the R7 scan from:

```csharp
shorter.Anchor.Count + 1
```

back to:

```csharp
shorter.Anchor.Count
```

so it examined the first hole again. All **1,083** maintained tests passed,
including the 66-test `NameShadowing`/`Shadows` surface. That is the exact
amplification FRESHAUDIT6 finding 3 added the name-bearing regression to catch.
Both deleted tests pass unchanged against the present production code when
restored temporarily, so this is a safeguard regression rather than a hidden
implementation failure.

**Recommendation:** restore both facts as independent semantic tests. The
six-order matrix should replace the old ordering and two-case message tests,
not the resolver-cost and first-hole tests that happened to sit between them in
the file.

### 3. The wall-clock R7 scaling test is flaky under the ordinary parallel suite

**Severity: low — a correct build can receive a false red from the maintained
gate**

`ValidatingAScopeDerivesThePatternRelationOnceNotOncePerName`
(`Test/Unit/Shadows.cs:310-350`) times one invocation at each size and requires
the 150-name run to take less than 25 times the one-name run. The baseline is
about 1.5 ms, and xUnit runs this class alongside other classes by default.

The first ordinary full-suite run in this audit failed:

```text
one name took 1.5 ms and 150 took 63.1 ms
Failed: 1, Passed: 1082
```

The same test passed immediately in isolation. A subsequent no-build full run
and the final rebuilt full run both passed, as did the instrumented Release
coverage run. That fail/pass sequence is evidence of scheduler/test-load
sensitivity, not an algorithm changing between runs.

The test's purpose is valid: line and branch coverage cannot constrain a
complexity regression. A single wall-clock sample inside a parallel unit suite
does not reliably enforce it, though, and a sporadic gate failure will train
people to rerun rather than investigate.

**Recommendation:** prefer a deterministic seam that counts refinement
derivations (one per validation, independent of name count), or move the
scaling assertion to a benchmark/performance gate. As an immediate mitigation,
put the timing test in a non-parallel collection and compare warmed medians
over several samples rather than two single measurements with a millisecond-
scale denominator.

## Verification performed

- Inspected both incorporating commits and their complete production/test diff.
- Re-ran the maintained six-order, structural-filter, pinned-hole/shifted-hole,
  renderer, and lookup-key surfaces.
- A temporary real-source nested-scope probe confirmed a local shorter pattern
  is blamed against inherited longer/name declarations and receives two related
  spans.
- Temporary restorations of the two deleted semantic tests passed against the
  implementation.
- A temporary first-hole mutation passed the focused 66-test diagnostic suite
  and the entire 1,083-test suite, reproducing finding 2's guard gap. The
  mutation was reverted.
- A temporary registry probe reproduced finding 1. All temporary test code was
  removed.
- `dotnet restore --locked-mode`: passed.
- `dotnet build --no-restore --configuration Release -warnaserror`: passed with
  zero warnings and zero errors.
- Exact Release coverage gate: **1,083 passed, 0 failed, 0 skipped; 100% line,
  branch, and method coverage**.
- Final rebuilt Debug suite: **1,083 passed, 0 failed, 0 skipped**.
- `git diff --check`: passed; `git diff -- Compiler Test` is empty after probe
  cleanup.

The pre-existing `docs/spec` edits and untracked handoff/design files were
preserved.

## Settled and deferred work

- The outstanding lookup representation/runtime work and the earlier finding 9
  remain explicitly deferred until that work lands; they are not recast here.
- The future differential declaration check, multi-word operator work, and
  other disclosed future slices remain outside this incorporation audit.
- Owner-authorized warning suppressions remain for their dedicated round.
- The documented hand-aligned `dotnet format` whitespace differences remain
  settled project style and are not a finding.
